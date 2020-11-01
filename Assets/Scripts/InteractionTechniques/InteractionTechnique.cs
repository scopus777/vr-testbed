using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Base class of an interaction technique which manages things all techniques share.
/// </summary>
public class InteractionTechnique : MonoBehaviour
{
    protected Hand primaryHand;

    public SteamVR_Action_Boolean pickupAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
    public bool moveHoverPoint = true;
    public AudioClip[] rightHandedInstructionsSelection;
    public AudioClip[] rightHandedInstructionsManipulation;
    public AudioClip[] leftHandedInstructionsSelection;
    public AudioClip[] leftHandedInstructionsManipulation;

    protected Vector3 newPrimaryHandPosition = Vector3.zero;
    protected Quaternion newPrimaryHandRotation = Quaternion.identity;
    protected Vector3 newSecondaryHandPosition = Vector3.zero;
    protected Quaternion newSecondaryHandRotation = Quaternion.identity;
    protected Vector3 newHeadPosition = Vector3.zero;
    protected Quaternion newHeadRotation = Quaternion.identity;
    
    protected virtual void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        
        // needed because somehow grabPinchAction and grabGripAction is null
        // after changing the hands for the next interaction technique
        primaryHand.grabPinchAction = TaskController.Instance.grabPinchAction;
        primaryHand.grabGripAction = TaskController.Instance.grabGripAction;
        
        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);
        SetNewHandPositionsAndRotations();
    }

    /// <summary>
    /// Is called by the TaskController by sending a message.
    /// </summary>
    /// <param name="targetPosition"></param>
    public virtual void TaskSolved(Vector3 targetPosition)
    {
        TaskController.Instance.TaskSolved(targetPosition);
    }

    public virtual void ReleaseObject()
    {
        if (primaryHand.currentAttachedObject != null)
            primaryHand.DetachObject(primaryHand.currentAttachedObject);
    }

    protected virtual void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        // in rare cases the grab pinch action in the hand is null after a technique change (even if the action is set in the inspector!)
        primaryHand.grabPinchAction = pickupAction;
        
        if (primaryHand.hoveringInteractable != null)
		    primaryHand.AttachObject(primaryHand.hoveringInteractable.gameObject, primaryHand.GetGrabStarting(), Hand.AttachmentFlags.DetachOthers | Hand.AttachmentFlags.ParentToHand );
        else
            TaskController.Instance.ObjectSelected(null, null, primaryHand);
    }

    protected virtual void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
    }

    protected virtual void LateUpdate()
    {
        AddFootprint();
    }

    protected virtual void SetNewHandPositionsAndRotations()
    {
        newPrimaryHandPosition = primaryHand.transform.position;
        newPrimaryHandRotation = primaryHand.transform.rotation;
        newSecondaryHandPosition = primaryHand.otherHand.transform.position;
        newSecondaryHandRotation = primaryHand.otherHand.transform.rotation;
        Transform head = TaskController.Instance.player.hmdTransform;
        newHeadPosition = head.position;
        newHeadRotation = head.rotation;
    }

    /// <summary>
    /// Adds the footprints.
    /// We cannot use Quaternion.Angle for tha rotation footprint calculation
    /// because then small rotations are not noticed.
    /// </summary>
    private void AddFootprint()
    {
        Vector3 lastPrimaryHandPosition = newPrimaryHandPosition;
        Quaternion lastPrimaryHandRotation = newPrimaryHandRotation;
        Vector3 lastSecondaryHandPosition = newSecondaryHandPosition;
        Quaternion lastSecondaryHandRotation = newSecondaryHandRotation;
        Vector3 lastHeadPosition = newHeadPosition;
        Quaternion lastHeadRotation = newHeadRotation;

        SetNewHandPositionsAndRotations();

        MeasurementController.Instance.AddFootprintPrimaryHand(
            Vector3.Distance(newPrimaryHandPosition, lastPrimaryHandPosition),
            Vector3.Angle(newPrimaryHandRotation * Vector3.forward, lastPrimaryHandRotation * Vector3.forward));
        MeasurementController.Instance.AddFootprintSecondaryHand(
            Vector3.Distance(newSecondaryHandPosition, lastSecondaryHandPosition),
            Vector3.Angle(newSecondaryHandRotation * Vector3.forward, lastSecondaryHandRotation * Vector3.forward));
        MeasurementController.Instance.AddFootprintHead(
            Vector3.Distance(newHeadPosition, lastHeadPosition),
            Vector3.Angle(newHeadRotation * Vector3.forward, lastHeadRotation * Vector3.forward));
    }

    public AudioClip[] GetInstructions()
    {
        return TaskController.Instance.IsRightHanded()
            ? TaskController.Instance.isSelectionStudy()
                ? rightHandedInstructionsSelection
                : rightHandedInstructionsManipulation
            : TaskController.Instance.isSelectionStudy()
                ? leftHandedInstructionsSelection
                : leftHandedInstructionsManipulation;
    }
}
