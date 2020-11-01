// Copied from Valve.VR.InteractionSystem.Throwable
// Removed all unnecessary properties and methods so the object can be taken and places somewhere without gravity

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Serialization;

namespace Valve.VR.InteractionSystem
{
	/// <summary>
	/// Adapted version of the SteamVR Throwable.
	/// Allows to take an object.
	/// </summary>
	public class Takeable : MonoBehaviour
	{
		[EnumFlags]
		[Tooltip( "The flags used to attach this object to the hand." )]
		public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.TurnOnKinematic;

        [Tooltip("The local point which acts as a positional and rotational offset to use while held")]
        public Transform attachmentOffset;

        [Tooltip( "When detaching the object, should it return to its original parent?" )]
		public bool restoreOriginalParent = false;

        protected bool attached = false;

		public UnityEvent onPickUp;
        public UnityEvent onDetachFromHand;
        public HandEvent onHeldUpdate;

        public bool preTaskObject;
        [HideInInspector] public TaskObject taskObject;
        

        //-------------------------------------------------
        /*protected virtual void HandHoverUpdate( Hand hand )
        {
	        // needed to prevent NullPointerException if pinch or grip action is not set
	        if (hand.grabPinchAction == null || hand.grabGripAction == null)
		        return;
	        
            GrabTypes startingGrabType = hand.GetGrabStarting();
            
            if (startingGrabType != GrabTypes.None)
            {
				hand.AttachObject( gameObject, startingGrabType, attachmentFlags, attachmentOffset );
            }
		}*/

        //-------------------------------------------------
        protected virtual void OnAttachedToHand( Hand hand )
		{
            attached = true;

			onPickUp.Invoke();

			hand.HoverLock( null );
			
			takeObject(hand);
		}

        public void takeObject(Hand hand)
        {
	        TaskController.Instance.ObjectSelected(preTaskObject ? null : taskObject, gameObject, hand);
        }


        //-------------------------------------------------
        protected virtual void OnDetachedFromHand(Hand hand)
        {
            attached = false;

            onDetachFromHand.Invoke();

            hand.HoverUnlock(null);

            ReleaseObject();
        }

        public void ReleaseObject()
        {
	        TaskController.Instance.ObjectReleased(taskObject, gameObject);
        }

        

        //-------------------------------------------------
        protected virtual void HandAttachedUpdate(Hand hand)
        {
	        // CHANGE: added check for scripted grab type -> means that the release of the object es
	        // managed separately and the object should not be released if the button is released
            if (hand.IsGrabEnding(this.gameObject) && GetGrabbingType(hand, this.gameObject) != GrabTypes.Scripted)
            {
                hand.DetachObject(gameObject, restoreOriginalParent);

                // Uncomment to detach ourselves late in the frame.
                // This is so that any vehicles the player is attached to
                // have a chance to finish updating themselves.
                // If we detach now, our position could be behind what it
                // will be at the end of the frame, and the object may appear
                // to teleport behind the hand when the player releases it.
                //StartCoroutine( LateDetach( hand ) );
            }

            if (onHeldUpdate != null)
                onHeldUpdate.Invoke(hand);
        }

        private GrabTypes GetGrabbingType(Hand hand, GameObject gameObject)
        {
	        for (int attachedObjectIndex = 0; attachedObjectIndex < hand.AttachedObjects.Count; attachedObjectIndex++)
	        {
		        if (hand.AttachedObjects[attachedObjectIndex].attachedObject == gameObject)
		        {
			        return hand.AttachedObjects[attachedObjectIndex].grabbedWithType;
		        }
	        }
	        return GrabTypes.None;
        }


        //-------------------------------------------------
        protected virtual IEnumerator LateDetach( Hand hand )
		{
			yield return new WaitForEndOfFrame();

			hand.DetachObject( gameObject, restoreOriginalParent );
		}


        //-------------------------------------------------
        protected virtual void OnHandFocusAcquired( Hand hand )
		{
			gameObject.SetActive( true );
		}


        //-------------------------------------------------
        protected virtual void OnHandFocusLost( Hand hand )
		{
			gameObject.SetActive( false );
		}
	}

    public enum ReleaseStyle
    {
        NoChange,
        GetFromHand,
        ShortEstimation,
        AdvancedEstimation,
    }
}
