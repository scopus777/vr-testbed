using Leap.Unity;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Hand = Leap.Hand;
using Leap;

public class VirtualHand : InteractionTechnique
{
    [Tooltip("Determines whether the leap motion or a vr controller is used.")]
    public bool useController;
    public LeapServiceProvider leapServiceProvider;
    public HandModelManager handModelManager;
    private Takeable grabbedObject;
    private Transform originalParent;

    private bool isGrabbing;
    private bool grabbedThisFrame;
    private float timeLastGrab;

    protected override void Start()
    {
        base.Start();
        
        // activate leap motion objects if necessary
        if (!useController)
        {
            leapServiceProvider.enabled = true;
            handModelManager.gameObject.SetActive(true);
            primaryHand.hoverSphereTransform.SetParent(transform);
            primaryHand.hoverSphereRadius = 0.05f;
            primaryHand.hoverSphereTransform.GetComponentInChildren<MeshRenderer>().enabled = false;
        }
    }

    private void Update()
    {
        if (!useController && leapServiceProvider.CurrentFrame.Hands.Count > 0 )
        {
            if (primaryHand.mainRenderModel)
                primaryHand.mainRenderModel.gameObject.SetActive(false);
            primaryHand.hoverSphereTransform.position = leapServiceProvider.CurrentFrame.Hands[0].PalmPosition.ToVector3();
            primaryHand.hoverSphereTransform.rotation = leapServiceProvider.CurrentFrame.Hands[0].Rotation.ToQuaternion();
            if (leapServiceProvider.CurrentFrame.Hands[0].GrabStrength > 0.9f )
            {
                if (!isGrabbing)
                {
                    grabbedThisFrame = true;
                    isGrabbing = true;
                }
            }
            else 
            {
                isGrabbing = false;
            }


            if (grabbedThisFrame && grabbedObject == null)
            {
                if (primaryHand.hoveringInteractable != null)
                {
                    Takeable toGrab = primaryHand.hoveringInteractable.GetComponent<Takeable>();
                    if (toGrab != null)
                    {
                        toGrab.takeObject(primaryHand);
                        if (!TaskController.Instance.IsSelectionTask())
                        {
                            grabbedObject = toGrab;
                            originalParent = grabbedObject.transform.parent;
                            grabbedObject.transform.SetParent(primaryHand.hoverSphereTransform);
                        }
                    }
                }
                else 
                {
                    TaskController.Instance.ObjectSelected(null, null, primaryHand);
                }
            }
            else if (!isGrabbing)
            {
                if (grabbedObject != null) 
                {
                    grabbedObject.transform.SetParent(originalParent);
                    grabbedObject.ReleaseObject();
                    grabbedObject = null;
                }   
            }
        }
        grabbedThisFrame = false;
    }

    protected override void SetNewHandPositionsAndRotations()
    {
        if (useController)
            base.SetNewHandPositionsAndRotations();
        else if (leapServiceProvider != null && leapServiceProvider.CurrentFrame != null)
        {
            foreach (Hand hand in leapServiceProvider.CurrentFrame.Hands)
            {
                if (IsPrimaryHand(hand))
                {
                    newPrimaryHandPosition = hand.Basis.translation.ToVector3();
                    newPrimaryHandRotation = CalculateRotation(hand.Basis);
                }
                else
                {
                    newSecondaryHandPosition = hand.Basis.translation.ToVector3();
                    newSecondaryHandRotation = CalculateRotation(hand.Basis);
                }
                Transform head = TaskController.Instance.player.hmdTransform;
                newHeadPosition = head.position;
                newHeadRotation = head.rotation;
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        leapServiceProvider.enabled = false;
        handModelManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks whether the leap motion hand is hte primary hand.
    /// </summary>
    private bool IsPrimaryHand(Hand hand)
    {
        if (TaskController.Instance.IsRightHanded() && hand.IsRight)
            return true;
        if (!TaskController.Instance.IsRightHanded() && hand.IsLeft)
            return true;
        return false;
    }
    
    /// <summary>
    /// From RiggedHand.cs
    /// </summary>
    private Quaternion CalculateRotation(LeapTransform trs) {
        Vector3 up = trs.yBasis.ToVector3();
        Vector3 forward = trs.zBasis.ToVector3();
        return Quaternion.LookRotation(forward, up);
    }
}
