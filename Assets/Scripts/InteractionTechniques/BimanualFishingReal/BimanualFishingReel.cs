using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class BimanualFishingReel : InteractionTechnique
{
    // Variables for SteamVR input
    public SteamVR_Action_Boolean pullAction;
    public SteamVR_Action_Boolean pushAction;
    public SteamVR_Action_Boolean shrinkAction;
    public SteamVR_Action_Boolean enlargeAction;
    public SteamVR_Action_Boolean rotateAction;

    // Variables for the hands
    private Hand secondaryHand;
    
    // Variables for Ray-Casting
    private LineRenderer renderedRay;
    private RaycastHit hit;
    private bool isHit;
    
    // Variables for object manipulation    
    private bool isPulling = false;
    private bool isPushing = false;
    private bool isShrinking = false;
    private bool isEnlarging = false;
    private float pullPushSpeed = 1f;
    private float shrinkEnlargeSpeed = 0.125f;

    // Variables for rotation
    private bool isRotating = false;
    private Quaternion oldRotNonDomHand;
    private Quaternion currentObjectRotation;

    protected override void Start()
    {
        // get hands
        primaryHand = TaskController.Instance.GetPrimaryHand();
        secondaryHand = primaryHand.otherHand;
        
        // find and activate ray on primary hand
        renderedRay = primaryHand.transform.Find("Ray").GetComponent<LineRenderer>();
        renderedRay.gameObject.SetActive(true);

        // ray is rendered in local space so the start position is always the position of the ray object
        renderedRay.SetPosition(0, Vector3.zero);

        // Add SteamVR action event listeners
        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);
        pullAction.AddOnChangeListener(OnPullActionChange, primaryHand.handType);
        pushAction.AddOnChangeListener(OnPushActionChange, primaryHand.handType);
        shrinkAction.AddOnChangeListener(OnShrinkActionChange, primaryHand.handType);
        enlargeAction.AddOnChangeListener(OnEnlargeActionChange, primaryHand.handType);
        rotateAction.AddOnChangeListener(OnRotateActionChange, secondaryHand.handType);
        
        SetNewHandPositionsAndRotations();
    }

    // Update is called once per frame
    void Update()
    {
        // check if ray hits object
        Vector3 rayDirection = primaryHand.transform.forward;
        isHit = Physics.Raycast(renderedRay.transform.position, rayDirection, out hit, Mathf.Infinity);

        // highlight object
        primaryHand.HoverLock(isHit ? hit.collider.GetComponent<Interactable>() : null);

        // Update length of rendered line to end at collision or after fixed length
        renderedRay.SetPosition(1,
            renderedRay.transform.InverseTransformDirection(rayDirection) * (primaryHand.currentAttachedObject
                ? Vector3.Distance(renderedRay.transform.position, primaryHand.currentAttachedObject.transform.position)
                : (isHit ? hit.distance : 10)));

        // Object manipulation
        if (primaryHand.currentAttachedObject != null)
        {
            if (isPulling)
            {
                // Don't move grabbed object into or behind controller
                Vector3 newPosition = primaryHand.currentAttachedObject.transform.position -
                                      rayDirection * (pullPushSpeed * Time.deltaTime);
                if (IsInFront(newPosition, primaryHand.transform))
                {
                    // Move grabbed object closer to the controller
                    primaryHand.currentAttachedObject.transform.position = newPosition;
                }
            }

            if (isPushing)
                // Move grabbed object away from controller
                primaryHand.currentAttachedObject.transform.Translate(rayDirection * (pullPushSpeed * Time.deltaTime),
                    Space.World);

            if (isShrinking)
            {
                // Don't inverse object scale by shrinking too much
                Vector3 newScale = primaryHand.currentAttachedObject.transform.localScale -
                                   Vector3.one * (shrinkEnlargeSpeed * Time.deltaTime);
                if (!(newScale.x < 0 ||
                      newScale.y < 0 ||
                      newScale.z < 0))
                {
                    primaryHand.currentAttachedObject.transform.localScale = newScale;
                }
            }

            if (isEnlarging)
            {
                // Add scale length to all axes
                primaryHand.currentAttachedObject.transform.localScale +=
                    Vector3.one * (shrinkEnlargeSpeed * Time.deltaTime);
            }

            if (isRotating)
            {
                // Translate controller rotation to object
                Quaternion deltaRotNonDomHand = secondaryHand.transform.rotation * Quaternion.Inverse(oldRotNonDomHand);
                oldRotNonDomHand = secondaryHand.transform.rotation;
                primaryHand.currentAttachedObject.transform.rotation =
                    deltaRotNonDomHand * primaryHand.currentAttachedObject.transform.rotation;
                currentObjectRotation = primaryHand.currentAttachedObject.transform.rotation;
            }
            else
            {
                primaryHand.currentAttachedObject.transform.rotation = currentObjectRotation;
            }
        }
    }

    private bool IsInFront(Vector3 newPosition, Transform transform)
    {
        Vector3 distanceVec = newPosition - transform.position;
        return Vector3.Dot(distanceVec, transform.forward) > 0;
    }

    protected override void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        if (primaryHand.currentAttachedObject == null)
        {
            if (isHit && hit.collider.gameObject.GetComponent<Interactable>() != null)
                GrabObject(hit.collider.gameObject);
            else
                base.PickupObject(fromaction, fromsource);
        }
        else
            ReleaseObject();
    }

    private void GrabObject(GameObject targetObject)
    {  
        Interactable interactable = targetObject.GetComponent<Interactable>();
        if (interactable != null)
        {
            primaryHand.AttachObject(interactable.gameObject, GrabTypes.Scripted, Hand.AttachmentFlags.ParentToHand, primaryHand.transform);
            // we immediately detach the object if it is a pre task object -> is already null in this line
            if (primaryHand.currentAttachedObject != null)
                currentObjectRotation = primaryHand.currentAttachedObject.transform.rotation;
        }
    }

    private void OnPullActionChange(SteamVR_Action_Boolean actionIn, SteamVR_Input_Sources inputSource, bool newValue)
    {
        isPulling = newValue;
    }

    private void OnPushActionChange(SteamVR_Action_Boolean actionIn, SteamVR_Input_Sources inputSource, bool newValue)
    {
        isPushing = newValue;
    }

    private void OnShrinkActionChange(SteamVR_Action_Boolean actionIn, SteamVR_Input_Sources inputSource, bool newValue)
    {
        isShrinking = newValue;
    }

    private void OnEnlargeActionChange(SteamVR_Action_Boolean actionIn, SteamVR_Input_Sources inputSource, bool newValue)
    {
        isEnlarging = newValue;
    }

    private void OnRotateActionChange(SteamVR_Action_Boolean actionIn, SteamVR_Input_Sources inputSource, bool newValue)
    {
        isRotating = newValue;
        oldRotNonDomHand = secondaryHand.transform.rotation;
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
        pullAction.RemoveOnChangeListener(OnPullActionChange, primaryHand.handType);
        pushAction.RemoveOnChangeListener(OnPushActionChange, primaryHand.handType);
        shrinkAction.RemoveOnChangeListener(OnShrinkActionChange, primaryHand.handType);
        enlargeAction.RemoveOnChangeListener(OnEnlargeActionChange, primaryHand.handType);
        rotateAction.RemoveOnChangeListener(OnRotateActionChange, secondaryHand.handType);
    }
}