using System.Collections.Generic;
using Leap;
using Leap.Unity;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Hand = Leap.Hand;

public class CrankHandle : InteractionTechnique
{
    [Tooltip("Determines whether the leap motion or a vr controller is used.")]
    public bool useController;
    public LeapServiceProvider leapServiceProvider;
    public HandModelManager handModelManager;
    public Transform Handle;
    public Material upLeverMaterial;
    public Material forwardLeverMaterial;
    public Material rightLeverMaterial;
    public Material barMaterial;
    
    [Tooltip("The grab strength needed to detect a grab. Only used when the leap motion is active.")]
    public float closeGrabStrength = 1;
     [Tooltip("Object is released if the hand is opened this time span.")]
    public float releaseGrabTimeWindow = 0.1f;
    [Tooltip("If the hand is closed again inside this time window the rotation mode is activated.")]
    public float rotationModeTimeWindow = 0.6f;
    [Tooltip("If the angle between the average normals which are calculated through the hand movements and a primary axis falls under this threshold the rotation is fixed to this axis.")]
    public float fixAxisAngleThreshold = 30f;
    [Tooltip("The minimum velocity needed to start rotation.")]
    public float minVelocityForRotation = 0.1f;
    [Tooltip("The velocity until the linear rotation increase is active.")]
    public float midVelocityForRotation = 0.65f;
    [Tooltip("The number of past positions considered for determine the rotation axis. Must be dividable by 3.")]
    public int rotationTrailSize = 9;
    [Tooltip("Frames which need to pass until the next position is considered for the rotation.")]
    public int pauseFrames = 2;
    [Tooltip("Loops needed to rotate an object by 360 degrees with the minimal velocity.")]
    public float minLoops = 50f;
    [Tooltip("Loops needed to rotate an object by 360 degrees with the mid velocity.")]
    public float midLoops = 30f;
    [Tooltip("Loops needed to rotate an object by 360 degrees with the minimal fast velocity.")]
    public float midMaxLoops = 10f;
    [Tooltip("Maximal loops needed to rotate an object by 360 degrees.")]
    public float maxLoops = 2.5f;

    // jitter detection
    public float maxMovementPerFrame = 0.05f;
    public float maxAngle = 15f;
    public float minConfidence = 0.85f;

    private bool grabActive;
    private bool grabDisabled;
    private float lastFullGrabTime;
    private float controllerGrabStrength;
    private bool rotationMode;
    private int currentPauseFrames;
    private Takeable currentTakeable;
    private Vector3 startGrabHandPosition;
    private Vector3 startGrabObjectPosition;
    private List<Vector3> lastHandPositions;
    private Transform translationLever;
    private Transform rotationLever;
    private Transform upAxisLever;
    private Transform rightAxisLever;
    private Transform forwardAxisLever;
    private GameObject leapVelocityDummy;

    private Vector3 lastPos;
    
    // Start is called before the first frame update
    protected override void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        lastHandPositions = new List<Vector3>();
        currentPauseFrames = pauseFrames;

        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);
        pickupAction.AddOnStateUpListener(ReleaseObject, primaryHand.handType);
        
        // move handle to the left side if user is left handed
        if (!TaskController.Instance.IsRightHanded())
            Handle.localScale = new Vector3(1,-1,1);
        
        // activate leap motion objects if necessary
        leapServiceProvider.enabled = true;
        if (!useController)
        {
            handModelManager.gameObject.SetActive(true);
            leapVelocityDummy = new GameObject("LeapVelocityDummy");
            leapVelocityDummy.AddComponent<VelocityCalculation>();
            leapVelocityDummy.GetComponent<VelocityCalculation>().TimeWindow = 0.1f;
        }

        translationLever = Handle.Find("Translation Lever");
        rotationLever = Handle.Find("Rotation Lever");
        upAxisLever = rotationLever.Find("Lever Up");
        rightAxisLever = rotationLever.Find("Lever Right");
        forwardAxisLever = rotationLever.Find("Lever Forward");

        SetNewHandPositionsAndRotations();
    }

    private void ReleaseObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        controllerGrabStrength = 0;
    }

    protected override void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        controllerGrabStrength = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (!useController && primaryHand.mainRenderModel != null){
                primaryHand.mainRenderModel.gameObject.SetActive(false);
                primaryHand.hoverSphereTransform.gameObject.SetActive(false);
        }

        if (leapServiceProvider.CurrentFrame.Hands.Count > 0 || useController)
        {
                if (grabActive && (GetHandGrabStrength() < 1 - closeGrabStrength ||
                                   lastFullGrabTime + releaseGrabTimeWindow < Time.realtimeSinceStartup))
                    ReleaseObject();

                if (GetHandGrabStrength() >= closeGrabStrength)
                {
                    // only grab again if grab is not active and grab is not disabled (because pre selection object was grabbed before)
                    if (!grabActive && !grabDisabled)
                        Grab();

                    lastFullGrabTime = Time.realtimeSinceStartup;
                    grabActive = true;
                }

                if (currentTakeable != null)
                {
                    if (rotationMode)
                        Rotate();
                    else
                        Translate();
                }

                lastPos = GetHandPos();
        }
        else
        {
            ReleaseObject();
        }

        if (leapServiceProvider.CurrentFrame.Hands.Count > 0 && !useController)
            leapVelocityDummy.transform.position = leapServiceProvider.CurrentFrame.Hands[0].PalmPosition.ToVector3();
    }

    /// <summary>
    /// Moves the current active object.
    /// </summary>
    private void Translate()
    {
        currentTakeable.transform.position =
            startGrabObjectPosition + (GetHandPos() - startGrabHandPosition);
        Handle.position = currentTakeable.transform.position;
    }

    /// <summary>
    /// Rotates the current active object.
    /// </summary>
    private void Rotate()
    {
        // only rotate if the current frame is not skipped
        currentPauseFrames--;
        if (currentPauseFrames > 0 || Vector3.Distance(lastPos,GetHandPos()) > maxMovementPerFrame)
            return;
        currentPauseFrames = pauseFrames;
        
        // save the current hand position
        lastHandPositions.Add(GetHandPos());
        
        // get current hand velocity
        float velocity = GetHandVelocity();

        // rotate a object around a primary axis if there are enough past hand positions
        if (lastHandPositions.Count > (rotationTrailSize / 3) * 3)
        {
            // ensure that the number of past hand position does not exceed the rotationTrailSize
            lastHandPositions.RemoveAt(0);

            // don't rotate if hand is to slow
            if (velocity < minVelocityForRotation)
                return;

            // calculate the average normal vector of the last triples
            Vector3 normal = GetAverageNormalVector(lastHandPositions);

            // check if the angle between the average normal vector and a primary axis is smaller than the given treshould
            Vector3 rotationAxis = Vector3.zero;
            if (Vector3.Angle(Vector3.forward, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.forward;
            else if (Vector3.Angle(Vector3.back, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.back;
            else if (Vector3.Angle(Vector3.right, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.right;
            else if (Vector3.Angle(Vector3.left, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.left;
            else if (Vector3.Angle(Vector3.up, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.up;
            else if (Vector3.Angle(Vector3.down, normal) <= fixAxisAngleThreshold)
                rotationAxis = Vector3.down;

            // rotate object around the detected primary axis (if there is one)
            if (rotationAxis != Vector3.zero)
            {
                // determine the circle described by the hand movements
                Vector2 v1 = lastHandPositions[lastHandPositions.Count - 3].ToVector2(rotationAxis);
                Vector2 v2 = lastHandPositions[lastHandPositions.Count - 2].ToVector2(rotationAxis);
                Vector2 v3 = lastHandPositions[lastHandPositions.Count - 1].ToVector2(rotationAxis);
                CircleHelper.FindCircle(v1, v2, v3, out Vector2 center, out float radius);

                // calculate the moved angle
                Vector2 c_v2 = v2 - center;
                Vector2 c_v3 = v3 - center;
                float angle = Vector2.Angle(c_v2, c_v3);

                if (!float.IsNaN(angle))
                {
                    float factor;
                    // linearly increase rotation amount for the first velocity window
                    if (velocity < midVelocityForRotation)
                        factor = minLoops + ((velocity - minVelocityForRotation) /
                                             (midVelocityForRotation - minVelocityForRotation)) * (midLoops - minLoops);
                    // exponentially increase rotation amount for the second velocity window
                    else
                    {
                        float cmVelocity = (velocity - midVelocityForRotation) * 100;
                        factor = Mathf.Max(midMaxLoops - cmVelocity * cmVelocity * 0.0005f, maxLoops);
                    }

                    // don't rotate if jitter is detected
                    if (angle > maxAngle && GetTrackingConfidence() > minConfidence)
                        return;
                    
                    angle = angle / factor;

                    currentTakeable.transform.RotateAround(currentTakeable.transform.position, rotationAxis, angle);

                    ManageRotationLever(rotationAxis, angle);
                }
            }
        }
    }

    /// <summary>
    /// Rotates the rotation lever for the given rotation axis and controls the transparency.
    /// </summary>
    private void ManageRotationLever(Vector3 rotationAxis, float angle)
    {
        if (rotationAxis == Vector3.up || rotationAxis == Vector3.down)
        {
            upLeverMaterial.SetMainColorAlpha(1);
            forwardLeverMaterial.SetMainColorAlpha(0.5f);
            rightLeverMaterial.SetMainColorAlpha(0.5f);
            upAxisLever.transform.RotateAround(rotationLever.position, rotationAxis, angle);
        }
        else if (rotationAxis == Vector3.forward || rotationAxis == Vector3.back)
        {
            upLeverMaterial.SetMainColorAlpha(0.5f);
            forwardLeverMaterial.SetMainColorAlpha(1);
            rightLeverMaterial.SetMainColorAlpha(0.5f);
            forwardAxisLever.transform.RotateAround(rotationLever.position, rotationAxis, angle);
        }
        else if (rotationAxis == Vector3.right || rotationAxis == Vector3.left)
        {
            upLeverMaterial.SetMainColorAlpha(0.5f);
            forwardLeverMaterial.SetMainColorAlpha(0.5f);
            rightLeverMaterial.SetMainColorAlpha(1);
            rightAxisLever.transform.RotateAround(rotationLever.position, rotationAxis, angle);
        }
    }

    /// <summary>
    /// Calculates the average normal vector.
    /// Considers a given number of past hand position triples.
    /// </summary>
    private Vector3 GetAverageNormalVector(List<Vector3> positions)
    {
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < positions.Count; i += 3)
        {
            Vector3 v1 = positions[i] - positions[i + 1];
            Vector3 v2 = positions[i + 2] - positions[i + 1];
            normals.Add(Vector3.Cross(v2,v1));
        }
        Vector3 result = Vector3.zero;
        foreach (var normal in normals)
            result += normal;

        return result / normals.Count;
    }
    
    /// <summary>
    /// Grabs the targetable object if it is not a pre selection object.
    /// </summary>
    private void Grab()
    {
        if (TaskController.Instance.currentGameObjects.Count == 0)
            return;

        if (Time.realtimeSinceStartup - lastFullGrabTime <= rotationModeTimeWindow)
        {
            rotationMode = true;
            rotationLever.gameObject.SetActive(true);
        }

        Takeable takeable = TaskController.Instance.currentGameObjects[0].GetComponent<Takeable>();
        takeable.takeObject(primaryHand);

        if (!takeable.preTaskObject && takeable.GetComponent<Interactable>() != null)
        {
            currentTakeable = takeable;
            startGrabObjectPosition = currentTakeable.transform.position;
            barMaterial.SetMainColorAlpha(1);
            if (!rotationMode)
                translationLever.gameObject.SetActive(true);
        }
        else
        {
            // disable grab until the next release so the start task object is
            // not grabbed directly after the pre selection task object is grabbed
            grabDisabled = true;
        }

        startGrabHandPosition = GetHandPos();
            
    }

    /// <summary>
    /// Returns the position of the hand or the controller.
    /// </summary>
    private Vector3 GetHandPos()
    {
        return useController
            ? primaryHand.transform.position
            : leapServiceProvider.CurrentFrame.Hands[0].PalmPosition.ToVector3();
    }

    /// <summary>
    /// Returns the velocity of the hand or the controller.
    /// </summary>
    private float GetHandVelocity()
    {
        return useController
            ? primaryHand.GetComponent<VelocityCalculation>().TranslationVelocityAverage
            : leapVelocityDummy.GetComponent<VelocityCalculation>().TranslationVelocityAverage;
    }

    /// <summary>
    /// Returns the grab strength. Is always 1 of the controller button is pressed.
    /// </summary>
    private float GetHandGrabStrength()
    {
        return useController ? controllerGrabStrength : leapServiceProvider.CurrentFrame.Hands[0].GrabStrength;
    }

    /// <summary>
    /// Returns the tracking confidence. Is always 1 of the controller is used.
    /// </summary>
    private float GetTrackingConfidence()
    {
        return useController ? 1f : leapServiceProvider.CurrentFrame.Hands[0].Confidence;
    }

    /// <summary>
    /// Releases the current objects and resets the technique if necessary.
    /// </summary>
    public override void ReleaseObject()
    {
        grabActive = false;
        grabDisabled = false;
        rotationLever.gameObject.SetActive(false);
        translationLever.gameObject.SetActive(false);
        barMaterial.SetMainColorAlpha(0.5f);
        if (currentTakeable != null)
        {
            currentTakeable.ReleaseObject();
            currentTakeable = null;
            lastHandPositions = new List<Vector3>();
            if (rotationMode)
                lastFullGrabTime = 0f;
            rotationMode = false;
        }
        else
        {
            lastFullGrabTime = 0f;
        }
    }
    
    /// <summary>
    /// Called in TaskController when task objects are started to faded out.
    /// </summary>
    private void FadeOutAndRemoveTaskObjectsStart()
    {
        Handle.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called in TaskController when the task objects are created.
    /// </summary>
    private void NextTaskObjectsCreated(GameObject startTaskObject)
    {
        ResetHandle();
        Handle.gameObject.SetActive(true);
        Handle.position = startTaskObject.transform.position;
    }

    /// <summary>
    /// Resets the handle.
    /// </summary>
    private void ResetHandle()
    {
        upLeverMaterial.SetMainColorAlpha(0.5f);
        forwardLeverMaterial.SetMainColorAlpha(0.5f);
        rightLeverMaterial.SetMainColorAlpha(0.5f);
        barMaterial.SetMainColorAlpha(0.5f);
        forwardAxisLever.localRotation = Quaternion.identity;
        rightAxisLever.localRotation = Quaternion.identity;
        upAxisLever.localRotation = Quaternion.identity;
        rotationLever.gameObject.SetActive(false);
        translationLever.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
        pickupAction.RemoveOnStateUpListener(ReleaseObject, primaryHand.handType);
        if (leapServiceProvider)
            leapServiceProvider.enabled = false;
        if (handModelManager)
            handModelManager.gameObject.SetActive(false);
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
            }
            Transform head = TaskController.Instance.player.hmdTransform;
            newHeadPosition = head.position;
            newHeadRotation = head.rotation;
        }
    }

    public void SyncObjects()
    {
        if (TaskController.Instance.currentGameObjects.Count > 0)
            Handle.gameObject.SetActive(TaskController.Instance.currentGameObjects[0].activeSelf);
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
