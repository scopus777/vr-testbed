using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Implementation of the World in Miniature technique. Generates a miniature model of the
/// environment which is placed on the users secondary hand. The user can select, move and
/// rotate the miniature objects which are connected to the real objects.
/// </summary>
public class WorldInMiniatureTechnique : InteractionTechnique
{
    public Transform parentEnvironment;
    public Transform boundaryCube;
    public ParticleSystem correctAnimation;
    public Material[] originalMaterials;
    public Material[] WIMMaterials;
    [Range(0.01f,1f)]
    public float initialScale = 1;
    public SteamVR_Action_Vector2 scrollAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("TouchpadScroll");
    public Transform anchor;
    public Vector3 WIMOffsetVector = new Vector3(0, 0.1f, 0);
    public int WIMFloorLayer;
    [Tooltip("If enabled the grabbed object is not scaled if the WIM is scaled which effectively scales the grabbed object.")]
    public bool scale;
    
    private Hand secondaryHand;

    private BoxCollider boundaryCubeCollider;
    private Dictionary<GameObject, GameObject> taskObjectMap;

    private Vector3? lastHandPosition;
    private Vector3 startGrabHandPosition;
    private float startGrabScale;
    private Transform wimObjects;
    private Transform tmpScaleParent;
    private Transform wimRoom;
    private Transform wimFloor;
    
    protected override void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        secondaryHand = primaryHand.otherHand;
        // Ensure that the left controller is hidden
        secondaryHand.gameObject.AddComponent<HideController>();
        secondaryHand.FullyHideController();
        secondaryHand.useHoverSphere = false;
        
        boundaryCubeCollider = boundaryCube.GetComponent<BoxCollider>();
        InitMiniatureEnvironment();
        pickupAction.AddOnChangeListener(GrabPinchOnChange, primaryHand.handType);
        
        // add tmp object which is used for scaling
        tmpScaleParent = new GameObject().GetComponent<Transform>();
        tmpScaleParent.SetParent(transform.parent);
        
        SetNewHandPositionsAndRotations();
    }

    private void GrabPinchOnChange(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource, bool newstate)
    {
        if (newstate)
        {
            // drag WIM if the right hand is in the boundary box and no object is highlighted or selected
            if (boundaryCubeCollider.bounds.Contains(primaryHand.hoverSphereTransform.position) &&
                primaryHand.hoveringInteractable == null && primaryHand.currentAttachedObject == null)
            {
                lastHandPosition = primaryHand.transform.position;
                startGrabHandPosition = primaryHand.transform.position;
                startGrabScale = transform.localScale.x;
            }
            else 
            {
                base.PickupObject(fromaction, fromsource);
            }
        }
        else
        {
            // if the the environment is not moved the selection is counted as a miss
            if (lastHandPosition != null &&
                Vector3.Distance(primaryHand.transform.position, startGrabHandPosition) < 0.02f &&
                Mathf.Abs(startGrabScale - transform.localScale.x) < 0.001)
            {
                Debug.Log(Vector3.Distance(primaryHand.transform.position, startGrabHandPosition) + " " + Mathf.Abs(startGrabScale - transform.localScale.x));
                base.PickupObject(fromaction, fromsource);
            }

            lastHandPosition = null;
        }
    }

    void Update()
    {
        // set shader properties
        for (int i = 0; i < WIMMaterials.Length; i ++)
        {
            // adopt size and position a little bit to avoid flickering of the wim floor
            WIMMaterials[i].SetVector("_BoundariesCenter", boundaryCube.position + new Vector3(0,-0.025f,0));
            WIMMaterials[i].SetVector("_BoundariesSize", boundaryCube.lossyScale + new Vector3(0,0.05f,0));
            WIMMaterials[i].SetVector("_DirVectorX", boundaryCube.right);
            WIMMaterials[i].SetVector("_DirVectorY", boundaryCube.up);
            WIMMaterials[i].SetVector("_DirVectorZ", boundaryCube.forward);
        }
        
        // set position of the WIM to the position of the left hand
        // the orientation of the WIM needs to be kept, that's why we cannot make the WIM a child of the left hand
        transform.parent.position = secondaryHand.transform.position + WIMOffsetVector;

        // determine scale value
        float scrollDirection = scrollAction.GetAxis(secondaryHand.handType).y;
        scrollDirection = float.IsNaN(scrollDirection) ? 0 : scrollDirection;
        float scrollValue = 0;
        
        if (transform.localScale.x >= initialScale || scrollDirection > 0)
            scrollValue = scrollDirection * Time.deltaTime;

        // scale grabbed object (objects loses parent while grabbed)
        if (primaryHand.currentAttachedObject != null)
        {
            if (!scale)
            {
                float scrollFactor = (transform.localScale.x + scrollValue) / transform.localScale.x;
                primaryHand.currentAttachedObject.transform.localScale *= scrollFactor;
            }
            else
            {
                taskObjectMap[primaryHand.currentAttachedObject].transform
                    .SetGlobalScale(primaryHand.currentAttachedObject.transform.lossyScale / transform.localScale.x);
            }
        }

        // scale WIM (around selection point if in boundary of the WIM or around centre)
        Vector3 newScale = transform.localScale + new Vector3(scrollValue, scrollValue, scrollValue);
        newScale = newScale.x < initialScale ? new Vector3(initialScale, initialScale, initialScale) : newScale; 
        if (boundaryCubeCollider.bounds.Contains(primaryHand.hoverSphereTransform.position))
        {
            Transform oldParent = transform.parent;
            tmpScaleParent.position = primaryHand.hoverSphereTransform.position;
            tmpScaleParent.localScale = transform.localScale;
            transform.SetParent(tmpScaleParent);
            tmpScaleParent.localScale = newScale;
            transform.SetParent(oldParent);
        }
        else
            transform.localScale = newScale;
        
        // drag WIM if the right hand is in the boundary box and no object is highlighted or selected
        if (lastHandPosition != null)
        {
            Vector3 moveVector = (Vector3) (primaryHand.transform.position - lastHandPosition);
            moveVector.y = 0;
            transform.position += moveVector;
            lastHandPosition = primaryHand.transform.position;
        }
        
        // moves the WIM so the anchor point is always inside the bounds
        Bounds bounds = getBounds();
        if (!bounds.Contains(anchor.position))
        {
            Vector3 point = bounds.ClosestPoint(anchor.position);
            Vector3 dir = anchor.position - point;
            transform.position += dir;
        }
        
        // correct position in case of scaling
        transform.position = new Vector3(transform.position.x, anchor.position.y, transform.position.z);

        // needs to be called in Update because the object childs need another frame to be initialized
        InitTaskObjectMap();
        
        // synchronize objects
        foreach (KeyValuePair<GameObject, GameObject> pair in taskObjectMap)
        {
            if (pair.Key == null || pair.Value == null)
                continue;
            pair.Value.transform.localPosition = (pair.Key.transform.position - transform.position) / transform.lossyScale.x;
            pair.Value.transform.localRotation = pair.Key.transform.rotation; 
            // needed to synchronize the active state after the initial object selection in a selection task
            pair.Key.SetActive(pair.Value.activeSelf);
        }
        
        // Debug
        if (Input.GetKey("o"))
            transform.localScale -= new Vector3(0.1f, 0.1f, 0.1f) * Time.deltaTime;
        if (Input.GetKey("p"))
            transform.localScale += new Vector3(0.1f, 0.1f, 0.1f) * Time.deltaTime;
        //
    }

    private void InitMiniatureEnvironment()
    {
        UpdateObjects();
        
        // set initial scale
        transform.localScale = new Vector3(initialScale,initialScale,initialScale);
    }

    /// <summary>
    /// Recreates the WIM environment.
    /// </summary>
    public void UpdateObjects()
    {
        taskObjectMap = new Dictionary<GameObject, GameObject>();
        
        // destroy all prior objects
        int childs = transform.childCount;
        for (int i = childs - 1; i >= 0; i--) 
            DestroyImmediate(transform.GetChild( i ).gameObject );
        
        // copy all objects from the parent environment
        foreach (Transform child in parentEnvironment)
            Instantiate(child, transform);

        // destroy all object which are not necessary (e.g. lights and the ceiling)
        foreach (Transform child in GetComponentsInChildren<Transform>())
            if (child.tag.Equals("NotForWIM"))
                Destroy(child.gameObject);

        // set wim floor and layer to correctly display position indicators in wim
        wimFloor = transform.FindDeepChild("Floor");
        wimFloor.gameObject.layer = WIMFloorLayer;
        
        // avoid interference when moving an object back into the environment
        foreach (Transform child in GetComponentsInChildren<Transform>())
            if (child.gameObject.layer == LayerMask.NameToLayer("Room"))
                child.gameObject.layer = LayerMask.NameToLayer("Default"); 
        
        // change materials with adapted shader (ensures that only the objects inside the bounding cube are visible)
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            SetWIMMaterial(meshRenderer);
        }
    }

    private void SetWIMMaterial(MeshRenderer meshRenderer)
    {
        for (int i = 0; i < originalMaterials.Length; i ++)
        {
            if (meshRenderer.sharedMaterial == originalMaterials[i])
            {
                meshRenderer.material = WIMMaterials[i];
            }
        }
    }

    /// <summary>
    /// Returns the real object mapped to the given WIM object.
    /// </summary>
    public GameObject GetMappedObject(GameObject wimObject)
    {
        return taskObjectMap[wimObject];
    }

    /// <summary>
    /// Returns all proxy objects.
    /// </summary>
    public List<GameObject> GetProxyObjects()
    {
        return taskObjectMap.Keys.ToList();
    }

    /// <summary>
    /// Updates the active state and positions of all WIM objects and destroys objects which do not
    /// exist in the real environment anymore. Is called by a Message from the TaskController.
    /// </summary>
    public void SyncObjects()
    {
        Dictionary<GameObject,GameObject> newTaskObjectMap = new Dictionary<GameObject, GameObject>();
        foreach (KeyValuePair<GameObject, GameObject> pair in taskObjectMap)
        {
            if (pair.Value == null)
            {
                Destroy(pair.Key);
                continue;
            }
            
            pair.Key.SetActive(pair.Value.activeSelf);
            pair.Key.transform.localPosition = pair.Value.transform.localPosition;
            newTaskObjectMap.Add(pair.Key,pair.Value);
        }
        taskObjectMap = newTaskObjectMap;
    }

    /// <summary>
    /// Fills the task object map. The key is the WIM object and the value the "real" object.
    /// </summary>
    private void InitTaskObjectMap()
    {
        wimObjects = transform.Find("Objects(Clone)");
        if (taskObjectMap.Count == 0 && wimObjects.childCount > 0)
        {
            Transform parentObjects = parentEnvironment.Find("Objects");
            for (int i = 0; i < parentObjects.childCount; i++)
            {
                GameObject wimObject = wimObjects.GetChild(i).gameObject;
                GameObject realObject = parentObjects.GetChild(i).gameObject;
                // map wim objects to the real objects
                taskObjectMap.Add(wimObject, realObject);
                // ensure that the real objects are not interactable anymore
                Destroy(realObject.GetComponent<Takeable>());
                Destroy(realObject.GetComponent<Interactable>());
                ShowPosition showPosition = wimObject.GetComponent<ShowPosition>();
                if (showPosition)
                {
                    showPosition.layer = LayerMask.GetMask("WIMFloor");
                    showPosition.floor = wimFloor.gameObject;
                    showPosition.currentBall.transform.SetParent(transform);
                    showPosition.currentBall.transform.localScale = Vector3.one;
                    SetWIMMaterial(showPosition.currentBall.GetComponentInChildren<MeshRenderer>());
                }
            }
        }
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnChangeListener(GrabPinchOnChange, primaryHand.handType);
    }

    public override void TaskSolved(Vector3 targetPosition)
    {
        correctAnimation.Clear();
        correctAnimation.transform.position = primaryHand.hoverSphereTransform.position;
        correctAnimation.Play();
        TaskController.Instance.TaskSolved(targetPosition, false);
    }

    /// <summary>
    /// Determines the bounds containing tha walls of the WIM room
    /// </summary>
    private Bounds getBounds()
    {
        wimRoom = transform.FindDeepChild("Walls");
        
        Vector3 minVector = new Vector3(Mathf.Infinity,Mathf.Infinity,Mathf.Infinity);
        Vector3 maxVector = new Vector3(Mathf.NegativeInfinity,Mathf.NegativeInfinity,Mathf.NegativeInfinity);
        foreach (Transform obj in wimRoom)
        {
            maxVector = Vector3.Max(maxVector, obj.position + obj.lossyScale / 2);
            minVector = Vector3.Min(minVector, obj.position - obj.lossyScale / 2);
        }
        Bounds bounds = new Bounds((maxVector + minVector) / 2, maxVector-minVector);

        return bounds;
    }
    
    private void FadeOutAndRemoveTaskObjectsEnd()
    {
        // is needed to avoid visual visual problems with the block object
        WIMMaterials.Where(m => m.name.Equals("Block")).ForEach(m => m.SetMaterialRenderingMode(MaterialExtension.BlendMode.Cutout));
        // don't highlight objects after task is solved
        taskObjectMap.Keys.ForEach(go => Destroy(go.GetComponent<Interactable>()));
    }

    private void FadeOutAndRemoveTaskObjectsStart()
    {
        WIMMaterials.Where(m => m.name.Equals("Block")).ForEach(m => m.SetMaterialRenderingMode(MaterialExtension.BlendMode.Fade));
    }
}
