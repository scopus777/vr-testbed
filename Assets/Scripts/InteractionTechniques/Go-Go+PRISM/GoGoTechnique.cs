using UnityEngine;

public class GoGoTechnique : InteractionTechnique
{
    // General SteamVR object variables
    public GameObject hmd;

    // Cursor and cursor movement variables 
    private Vector3 deltaHandPos;
    private Vector3 oldHandPos;
    
    // PRISM threshold values in m/s
    private const float MINV = 0.001f, SC = 0.3f, MAXV = 0.5f;
    
    // Go-Go values
    private const float targetDistanceInCM = 720f;
    private float gogoD;
    private float gogoK;

    private VelocityCalculation velocity;
    private float prismOffsetRecoveryStartTime;
    private Transform steamVRObjects;
    private Transform cursor;
    private float maxDistance;
    private Vector3 chestHeadOffset;
    private bool initialized;
    private Vector3 cursorPrismPosition;
    private Vector3 prismOffsetRecoveryVector;
    private Vector3 prismOffsetVector;
    private Quaternion initRotationDiff;
    private Vector3 initPosDiff;
    private Vector3 initChestPosition;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        oldHandPos = primaryHand.transform.position;
        velocity = primaryHand.GetComponent<VelocityCalculation>();
        
        steamVRObjects = GameObject.Find("SteamVRObjects").transform;

        initialized = false;
        StatusTextController.Instance.UpdateUserActionRequest("Stretch out arms!");
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
            return;
        
        deltaHandPos = primaryHand.transform.position - oldHandPos;

        // velocity calculated in a predefined time window
        float currentVelocity = velocity.TranslationVelocityFixed;

        // recover offset if max velocity is exceeded
        if (currentVelocity > MAXV)
        {
            // determine factor so that offset is recovered linearly within a second
            float factor = Mathf.Min(Time.realtimeSinceStartup - prismOffsetRecoveryStartTime, 1);

            // finish recovery after 1 second 
            if (factor < 1)
                prismOffsetVector = prismOffsetRecoveryVector * (1 - factor);
            else
                prismOffsetVector = Vector3.zero;
            cursorPrismPosition = primaryHand.transform.position + prismOffsetVector;
        }
        else
        {
            // don't change position if minimum velocity is not exceeded (nearly impossible to move the hand that slow...)
            if (currentVelocity > MINV)
            {
                // scale the directional vector from the old position to the new position and add the vector to the
                // old position if the velocity of the hand is greater than the scaling constant
                if (currentVelocity < SC)
                {
                    cursorPrismPosition += (currentVelocity / SC) * deltaHandPos;
                    prismOffsetVector = cursorPrismPosition - primaryHand.transform.position;
                    
                }
                // don't scale if the scaling value is not exceeded
                else
                {
                    cursorPrismPosition = primaryHand.transform.position + prismOffsetVector;
                }
            }
            
            // set values for potential offset recovery next frame
            prismOffsetRecoveryVector = prismOffsetVector;
            prismOffsetRecoveryStartTime = Time.realtimeSinceStartup;
        }
        
        cursor.position = initPosDiff + CalculateGoGoPosition(cursorPrismPosition);
        cursor.rotation = primaryHand.transform.rotation * initRotationDiff;
        oldHandPos = primaryHand.transform.position;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        // sets the parent of an attached object to the cursor so it moves along with the cursor
        if (primaryHand.currentAttachedObject != null)
            primaryHand.currentAttachedObject.transform.SetParent(cursor);
    }

    /// <summary>
    /// Calculates the Go-Go hand position for the given real hand position. 
    /// </summary>
    private Vector3 CalculateGoGoPosition(Vector3 handPos)
    {
        Vector3 chestPosition = hmd.transform.position + chestHeadOffset; 
        float chestHandDistance = Vector3.Distance(initChestPosition, handPos);
        Vector3 chestHandDirection = (handPos - initChestPosition).normalized;
        return initChestPosition + ComputeGoGoDistance(chestHandDistance) * chestHandDirection;
    }

    /// <summary>
    /// Implementation of Go-Go mapping function according to Poupyrev et al.
    /// The original formula uses cm so we need to convert our values in cm.
    /// </summary>
    private float ComputeGoGoDistance(float realHandChestDistance)
    {
        if (realHandChestDistance < gogoD)
            return realHandChestDistance;
        return realHandChestDistance + gogoK * (Mathf.Pow((realHandChestDistance - gogoD) * 100, 2) / 100f);
    }

    /// <summary>
    /// Determines the coefficient k for the Go-Go technique. This allows every user to reach the target
    /// distance if the arm is fully extended no matter how long their arm is.
    /// </summary>
    private float GetGoGoK()
    {
        float maxDistanceInCM = maxDistance * 100;
        return (targetDistanceInCM - maxDistanceInCM) / Mathf.Pow(maxDistanceInCM - (2f / 3f) * maxDistanceInCM, 2);
    }

    /// <summary>
    /// Initializes the technique.
    /// </summary>
    public bool InitTechnique()
    {
        if (initialized)
            return true;
        
        // determine initial values for Go-Go and PRISM
        chestHeadOffset = new Vector3(0,primaryHand.transform.position.y - hmd.transform.position.y,0);
        Vector3 currentChestPosition = hmd.transform.position + chestHeadOffset;
        maxDistance = Vector3.Distance(currentChestPosition,primaryHand.transform.position);
        cursor = primaryHand.transform.Find("Cursor");
        initRotationDiff =  cursor.localRotation;
        initPosDiff = cursor.position - primaryHand.transform.position;
        cursor.SetParent(steamVRObjects);
        primaryHand.hoverSphereTransform.SetParent(cursor);
        oldHandPos = cursorPrismPosition = primaryHand.transform.position;
        gogoD = 2f/3f * maxDistance;
        gogoK = GetGoGoK();
        StatusTextController.Instance.UpdateUserActionRequest("");
        initChestPosition = hmd.transform.position + chestHeadOffset;

        initialized = true;
        return false;
    }

    protected override void OnDisable(){
        base.OnDisable();
        if (cursor != null)
            Destroy(cursor.gameObject);
    }
}
