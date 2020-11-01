using UnityEngine;
using UnityEngine.Serialization;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Scaled HOMER technique according to:
/// Wilkes, Curtis; Bowman, Doug A. (2008): Advantages of velocity-based scaling for distant 3D manipulation
///
/// If "scaleHandPosition" the technique behaves as the normal HOMER technique.
/// </summary>
public class ScaledHomer : InteractionTechnique
{
    public GameObject hmd;

    [Tooltip("If active the influence of the vector offset is reduced when the user moves his hand closer to his torso.")]
    public bool scaleVectorOffset = true;
    [Tooltip("If active the real hand position is scaled before HOMER is applied. This is the difference between Scaled HOMER and normal HOMER.")]
    public bool scaleHandPosition = true;
    [Tooltip("Determines how much the movements of the hand are reduced.")]
    public float scalingConstantTranslation = 1.5f;
    [Tooltip("The minimum velocity needed to move the object.")]
    public float minVelocityTranslation = 0.01f;
    [Tooltip("Activates scaling with the help of the second hand.")]
    public bool secondHandScale = false;
    [Tooltip("Activates scaling of rotation.")]
    public bool scaleRotation = false;
    [Tooltip("Determines how much the rotation of the hand are reduced.")]
    public float scalingConstantRotation = 30f;
    [Tooltip("The minimum velocity needed to rotate the object.")]
    public float minVelocityRotation = 3f;

    private VelocityCalculation velocity;
    private LineRenderer renderedRay;
    private RaycastHit hit;

    private Vector3 vectorOffset;
    private float initObjDistance;
    private float initHandDistance;
    private Vector3 initHandPosition;
    private Vector3 initTorsoPosition;
    private float? initHandToHandDistance;
    private float initObjScale;

    private Takeable grabbedObject;

    private Vector3 lastHandPos;
    private Vector3 lastScaledHandPos;
    private Quaternion initHandRot;
    private Quaternion initObjRot;
    
    // Start is called before the first frame update 
    protected override void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        velocity = primaryHand.GetComponent<VelocityCalculation>();

        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);
        pickupAction.AddOnStateUpListener(PickupButtonReleased, primaryHand.handType);
        if (secondHandScale)
            pickupAction.AddOnChangeListener(FunctionToCall, primaryHand.otherHand.handType);
        
        renderedRay = primaryHand.transform.Find("Ray").GetComponent<LineRenderer>();
        renderedRay.gameObject.SetActive(true);

        // ray is rendered in local space so the start position is always the position of the ray object
        renderedRay.SetPosition(0, Vector3.zero);
        
        SetNewHandPositionsAndRotations();
    }

    private void FunctionToCall(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource, bool newstate)
    {
        if (newstate && grabbedObject != null)
        {
            initHandToHandDistance =
                Vector3.Distance(primaryHand.transform.position, primaryHand.otherHand.transform.position);
            initObjScale = grabbedObject.transform.localScale.x;
        }
        else
            initHandToHandDistance = null;
    }

    private void PickupButtonReleased(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        if (grabbedObject != null)
        {
            ReleaseObject();
        }
    }

    protected override void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        if (primaryHand.hoveringInteractable)
        {
            Takeable takeable = primaryHand.hoveringInteractable.GetComponent<Takeable>();

            if (takeable.preTaskObject){
                takeable.takeObject(primaryHand);
                return;
            }
            
            grabbedObject = takeable;
            
            initTorsoPosition = hmd.transform.position - new Vector3(0, 0.25f, 0);
            initObjDistance = getDistanceFromTorso(grabbedObject.transform.position);
            initHandDistance = getDistanceFromTorso(primaryHand.transform.position);
            initHandPosition = primaryHand.transform.position;
            vectorOffset = grabbedObject.transform.position - (initTorsoPosition + 
                           initObjDistance * ((initHandPosition - initTorsoPosition) / initHandDistance));
            renderedRay.gameObject.SetActive(false);

            lastScaledHandPos = primaryHand.transform.position;
            initHandRot = primaryHand.transform.rotation;
            initObjRot = grabbedObject.transform.rotation;
            
            takeable.takeObject(primaryHand);
        }
        else
        {
            base.PickupObject(fromaction, fromsource);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!grabbedObject)
        {
            // check if ray hits object
            Vector3 rayDirection = primaryHand.transform.forward;
            bool isHit = Physics.Raycast(renderedRay.transform.position, rayDirection, out hit, Mathf.Infinity);

            // highlight object
            primaryHand.HoverLock(isHit ? hit.collider.GetComponent<Interactable>() : null);

            // Update length of rendered line to end at collision or after fixed length
            renderedRay.SetPosition(1,
                renderedRay.transform.InverseTransformDirection(rayDirection) * (primaryHand.currentAttachedObject
                    ? Vector3.Distance(renderedRay.transform.position,
                        primaryHand.currentAttachedObject.transform.position)
                    : (isHit ? hit.distance : 10)));
        }
        else
        {
            // calculate scaled hand position
            float currVelo = velocity.TranslationVelocityAverage > minVelocityTranslation ? velocity.TranslationVelocityAverage : 0;
            Vector3 movedVector = primaryHand.transform.position - lastHandPos;
            float scaledHandMoveDistance = Mathf.Min(currVelo / scalingConstantTranslation, 1.2f) * movedVector.magnitude;
            Vector3 scaledCurrHandPosition = scaledHandMoveDistance * movedVector.normalized + lastScaledHandPos;
            Vector3 currHandPosition = scaleHandPosition ? scaledCurrHandPosition : primaryHand.transform.position;

            // calculate the obj position according to the HOMER technique
            float currHandDis = getDistanceFromTorso(currHandPosition);
            float virtHandDis = currHandDis * (initObjDistance / initHandDistance);
            float vectorOffsetFactor = scaleVectorOffset ? currHandDis / initHandDistance : 1;
            grabbedObject.transform.position = initTorsoPosition + virtHandDis *
                                               ((currHandPosition - initTorsoPosition) / initHandDistance) +
                                               vectorOffset * vectorOffsetFactor;

            // apply hand rotation to the object rotation (and scale the rotation if activated)
            if (scaleRotation)
            {
                float currRotVelo = velocity.RotationVelocityAverage > minVelocityRotation ? velocity.RotationVelocityAverage : 0;
                Quaternion diffRot = primaryHand.transform.rotation * Quaternion.Inverse(initHandRot);
                grabbedObject.transform.rotation = Quaternion.LerpUnclamped(Quaternion.identity,diffRot, Mathf.Min(currRotVelo / scalingConstantRotation, 2f)) * initObjRot;
                // init values are here the current values
                initHandRot = primaryHand.transform.rotation;
                initObjRot = grabbedObject.transform.rotation;
            }
            else
            {
                Quaternion diffRot = primaryHand.transform.rotation * Quaternion.Inverse(initHandRot);
                grabbedObject.transform.rotation = diffRot * initObjRot;
                
            }
            // scale object
            if (initHandToHandDistance != null)
            {
                float currentHandToHandDistance = Vector3.Distance(primaryHand.transform.position,
                    primaryHand.otherHand.transform.position);
                float newScale = initObjScale + (currentHandToHandDistance - (float) initHandToHandDistance);
                grabbedObject.transform.localScale = new Vector3(newScale,newScale,newScale);
            }

            lastScaledHandPos = scaledCurrHandPosition;
        }
        lastHandPos = primaryHand.transform.position;
    }

    private float getDistanceFromTorso(Vector3 pos)
    {
        return Vector3.Distance(pos, initTorsoPosition);
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
        pickupAction.RemoveOnStateUpListener(PickupButtonReleased, primaryHand.handType);
        pickupAction.RemoveOnChangeListener(FunctionToCall, primaryHand.otherHand.handType);
    }

    public override void ReleaseObject()
    {
        if (grabbedObject)
            grabbedObject.ReleaseObject();
        renderedRay.gameObject.SetActive(true);
        grabbedObject = null;
    }
}
