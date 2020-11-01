using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Handle Bar technique with controllers instead of hands and without pitch rotating with the bicycle gesture.
/// </summary>
public class HandleBar : InteractionTechnique
{
    public Transform center;

    private Hand secondaryHand;
    private Takeable grabbedObject;
    private Transform originalParent;
    private bool pickupPressedPrimaryHand;
    private bool pickupPressedSecondaryHand;

    private float initialDistance;

    protected override void Start()
    {
        // init hands 
        primaryHand = TaskController.Instance.GetPrimaryHand();
        secondaryHand = primaryHand.otherHand;

        // add listener for both hands
        pickupAction.AddOnChangeListener(SetPickupPressedPrimaryHand, primaryHand.handType);
        pickupAction.AddOnChangeListener(SetPickupPressedSecondaryHand, secondaryHand.handType);

        SetNewHandPositionsAndRotations();
    }

    private void SetPickupPressedPrimaryHand(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource,
        bool newstate)
    {
        pickupPressedPrimaryHand = newstate;
        GrabOrReleaseObject();
    }

    private void SetPickupPressedSecondaryHand(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource,
        bool newstate)
    {
        pickupPressedSecondaryHand = newstate;
        GrabOrReleaseObject();
    }

    private void GrabOrReleaseObject()
    {
        // grab object of both grab buttons are pressed
        if (pickupPressedPrimaryHand && pickupPressedSecondaryHand)
        {
            // Record a miss if no object is selected
            if (primaryHand.hoveringInteractable == null)
            {
                TaskController.Instance.ObjectSelected(null, null, primaryHand);
                return;
            }
            
            // set grabbed object
            grabbedObject = primaryHand.hoveringInteractable.GetComponent<Takeable>();

            // disable hover effect
            primaryHand.HoverLock(null);

            // set parent of the grabbed object to the selection sphere if it is not a pre task object
            if (!grabbedObject.preTaskObject)
            {
                originalParent = grabbedObject.transform.parent;
                grabbedObject.transform.SetParent(center.transform);
            }

            // handle object grabbing (start time measurement etc.)
            grabbedObject.takeObject(primaryHand);
            
            initialDistance = Vector3.Distance(primaryHand.transform.position, secondaryHand.transform.position);
        }
        // release object if currently grabbed
        else if (grabbedObject != null)
        {
            ReleaseObject();
        }
    }

    void Update()
    {
        // set position and rotation of the center
        Vector3 handToHandDir = primaryHand.transform.position - secondaryHand.transform.position;
        center.transform.rotation = Quaternion.LookRotation(handToHandDir, Vector3.up);
        center.transform.position = secondaryHand.transform.position + handToHandDir / 2;

        if (grabbedObject != null)
        {
            float distance = Vector3.Distance(primaryHand.transform.position, secondaryHand.transform.position);
            float newScale = distance / initialDistance;
            // change the scale of the grabbed object 
            center.transform.localScale =  new Vector3(newScale, newScale, newScale);
        }
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnChangeListener(SetPickupPressedPrimaryHand, primaryHand.handType);
        pickupAction.RemoveOnChangeListener(SetPickupPressedSecondaryHand, secondaryHand.handType);
    }

    public override void ReleaseObject()
    {
        if (grabbedObject)
        {
            grabbedObject.transform.SetParent(originalParent);
            grabbedObject.ReleaseObject();
            grabbedObject = null;
        }

        center.transform.localScale = Vector3.one;
    }
}
