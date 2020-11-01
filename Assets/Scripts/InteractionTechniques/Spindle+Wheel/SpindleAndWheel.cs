using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SpindleAndWheel : InteractionTechnique
{
    public SelectionSphere selectionSphere;
    public Transform bar;
    [Tooltip("Determines whether the primary hand can rotate an object. This distinguishes Spindle + Wheel and Spindle.")]
    public bool rotationActive;

    private Hand secondaryHand;
    private Takeable grabbedObject;
    private Transform originalParent;
    private float newScale;
    private float lastScale;
    private Quaternion newRotation;
    private Quaternion oldRotation;
    private bool pickupPressedPrimaryHand;
    private bool pickupPressedSecondaryHand;

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
            if (selectionSphere.hoveringInteractable == null)
            {
                TaskController.Instance.ObjectSelected(null, null, primaryHand);
                return;
            }
            
            // set grabbed object
            grabbedObject = selectionSphere.hoveringInteractable.GetComponent<Takeable>();

            // disable hover effect
            primaryHand.HoverLock(null);

            // set parent of the grabbed object to the selection sphere if it is not a pre task object
            if (!grabbedObject.preTaskObject)
            { 
                originalParent = grabbedObject.transform.parent;
                grabbedObject.transform.SetParent(selectionSphere.transform);
            }

            // handle object grabbing (start time measurement etc.)
            grabbedObject.takeObject(primaryHand);
        }
        // release object if currently grabbed
        else if (grabbedObject != null)
        {
            ReleaseObject();
        }
    }

    void Update()
    {
        // show hover effect if no object is grabbed
        if (grabbedObject == null)
            primaryHand.HoverLock(selectionSphere.hoveringInteractable);

        // set position and rotation of the selection sphere and the bar
        Vector3 handToHandDir = primaryHand.transform.position - secondaryHand.transform.position;
        selectionSphere.transform.rotation = Quaternion.LookRotation(handToHandDir, selectionSphere.transform.up);
        selectionSphere.transform.position = secondaryHand.transform.position + handToHandDir / 2;

        // set scale of the bar so it fits between the two hands
        newScale = (Vector3.Distance(primaryHand.transform.position, secondaryHand.transform.position) /
                    selectionSphere.transform.lossyScale.y) / 2;
        bar.localScale = new Vector3(0.25f, newScale, 0.25f);

        // save rotation of the primary hand
        newRotation = primaryHand.transform.rotation;

        if (grabbedObject != null)
        {
            // change the scale of the grabbed object 
            float scaleOffset = newScale - lastScale;
            Vector3 objScale = grabbedObject.transform.localScale + new Vector3(scaleOffset, scaleOffset, scaleOffset);
            grabbedObject.transform.ScaleAround(selectionSphere.transform.position, objScale);
            
            // rotate the grabbed object by the amount the primary hand is rotated around the z axis of the selection sphere
            if (rotationActive)
            {
                Quaternion relative = newRotation * Quaternion.Inverse(oldRotation);
                Vector3 newDir = relative * selectionSphere.transform.right;
                float zAngle = Vector3.SignedAngle(selectionSphere.transform.right, newDir,
                    selectionSphere.transform.forward);
                grabbedObject.transform.RotateAround(selectionSphere.transform.position,
                    selectionSphere.transform.forward,
                    zAngle);
            }
        }

        // save scale and rotation for the next frame
        lastScale = newScale;
        oldRotation = newRotation;
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnChangeListener(SetPickupPressedPrimaryHand, primaryHand.handType);
        pickupAction.RemoveOnChangeListener(SetPickupPressedSecondaryHand, secondaryHand.handType);
    }

    public override void ReleaseObject()
    {
        if (!grabbedObject)
            return;
        
        grabbedObject.transform.SetParent(originalParent);
        grabbedObject.ReleaseObject();
        grabbedObject = null;
    }
}
