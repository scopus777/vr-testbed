using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Expand : InteractionTechnique
{
    public SelectionCone selectionCone;
    public Transform grid;
    public Transform head;
    public Material[] materialMap;
    public float spaceBetweenObjects = 0.025f;
    public float transitionDuration = 0.5f;
    public float gridDistanceFromUser = 0.5f;

    private bool inTransition;
    private Dictionary<GameObject, GameObject> objMap;
    private Vector3 lastSelectedObjPos;

    protected override void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        selectionCone.transform.parent.SetParent(primaryHand.transform);
        selectionCone.transform.parent.localPosition = Vector3.zero;
        selectionCone.transform.parent.rotation = primaryHand.transform.rotation;
        
        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);

        SetNewHandPositionsAndRotations();
    }

    /// <summary>
    /// Called when the pickup button is pressed.
    /// </summary>
    protected override void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        // allow ne selection if we are in the transition between the normal phase and the grid phase
        if (inTransition)
            return;
        
        // if objects are not arranged in a grid
        if (!selectionCone.inGridPhase)
        {   
            // do nothing if no object is in the selection cone
            if (selectionCone.interactablesInSelection.Count == 0)
            {
                base.PickupObject(fromaction, fromsource);
                return;
            }

            // arrange object in a grid if more then one object is in the selection cone
            if (selectionCone.interactablesInSelection.Count > 1)
                ArrangeObjects();
            // if there is only one object in the cone select it
            else
                SelectObject(selectionCone.interactablesInSelection[0]);
        }
        else
        {
            // return the objects to the original position if no object is in the selection cone
            if (selectionCone.interactablesInSelection.Count == 0)
            {
                base.PickupObject(fromaction, fromsource);
                ReturnObjects();
            }
            // if there is only one object in the cone select it (in grid mode at most 1 object can be in the selection cone)
            else
                SelectObject(selectionCone.interactablesInSelection[0]);
            
        }
    }

    /// <summary>
    /// Selects the given object and and ensures that all objects are deselected and removed from the selection cone.
    /// </summary>
    private void SelectObject(GameObject selectable)
    {
        lastSelectedObjPos = selectable.transform.position;
        selectable.GetComponent<Takeable>().takeObject(primaryHand);
        selectionCone.DeselectAll();
    }

    /// <summary>
    /// Arranges oll objects in the selection cone in grid.
    /// </summary>
    private void ArrangeObjects()
    {
        // ensure that arranged objects are visible
        ResetGridObjectsMaterials();

        // rotate the grid so is fits the rotation of the head
        grid.rotation = Quaternion.LookRotation(head.forward, Vector3.up);

        // set the position of the grid so it is centered in the view of the user
        int gridSize = (int) Mathf.Ceil(Mathf.Sqrt(selectionCone.interactablesInSelection.Count));
        grid.position = head.position + head.forward * gridDistanceFromUser;
        float moveOffset = (gridSize - 1) *
                           (spaceBetweenObjects + selectionCone.interactablesInSelection[0].transform.localScale.x) / 2;
        grid.position += new Vector3(-moveOffset, moveOffset, 0);

        // move copies of the objects in the selection cone smoothly to there grid position while the originals get transparent
        inTransition = true;
        Sequence sequence = DOTween.Sequence();
        sequence.AddMaterialsFade(materialMap.Where((e, i) => i % 2 == 0).ToList(), 0.25f, transitionDuration);
        objMap = new Dictionary<GameObject, GameObject>();
        List<GameObject> sortedInteractables = SortForGrid(selectionCone.interactablesInSelection);
        for (int i = 0; i < sortedInteractables.Count; i++)
        {
            GameObject clone = Instantiate(sortedInteractables[i].gameObject, grid, true);
            sequence.Join(clone.transform.DOLocalMove(GetGridObjectPosition(i, gridSize, clone.transform.lossyScale.x),
                transitionDuration));
            objMap.Add(sortedInteractables[i], clone);
        }

        // change materials of the grid objects so they stay solid when all other objects are transparent
        foreach (MeshRenderer meshRenderer in grid.GetComponentsInChildren<MeshRenderer>(true))
            for (int i = 0; i < materialMap.Length; i += 2)
                if (meshRenderer.sharedMaterial == materialMap[i])
                    meshRenderer.material = materialMap[i + 1];
        sequence.AppendCallback(() => inTransition = false);
        sequence.Play();

        // change to grid phase
        selectionCone.inGridPhase = true;
        
        // ensure that only the objects in the grid can be highlighted and selected
        selectionCone.selectableInteractables = objMap.Values.ToList();
        
        // deselect all objects which are not in the grid
        selectionCone.DeselectAll();
    }

    /// <summary>
    /// Moves all objects in the grid to there original position and delete them when they reached there target.
    /// </summary>
    private void ReturnObjects()
    {
        inTransition = true;
        selectionCone.active = false;
        Sequence sequence = DOTween.Sequence();
        foreach (KeyValuePair<GameObject, GameObject> keyValue in objMap)
            sequence.Join(keyValue.Value.transform.DOMove(keyValue.Key.transform.position, transitionDuration));
        sequence.AddMaterialsFade(materialMap.Where((e, i) => i % 2 == 0).ToList(), 1, transitionDuration, true);
        sequence.AppendCallback(delegate
        {
            inTransition = false;
            objMap.Values.ForEach(DestroyImmediate);
            objMap = new Dictionary<GameObject, GameObject>();
            selectionCone.Reset();
            selectionCone.selectableInteractables = TaskController.Instance.currentGameObjects;
        });
        sequence.Play();
        
    }
    
    /// <summary>
    /// Called in TaskController when task objects are started to faded out.
    /// </summary>
    private void FadeOutAndRemoveTaskObjectsStart()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AddMaterialsFade(materialMap.Where((e, i) => i % 2 == 1).ToList(), 0f, transitionDuration);
        sequence.Play();
        selectionCone.active = false;
        inTransition = true;
    }
    
    /// <summary>
    /// Called in TaskController when task objects are finished to faded out.
    /// </summary>
    private void FadeOutAndRemoveTaskObjectsEnd()
    {
        selectionCone.Reset();
        selectionCone.selectableInteractables = TaskController.Instance.currentGameObjects;
        inTransition = false;
    }

    /// <summary>
    /// Makes the grid objects completely visible.
    /// </summary>
    private void ResetGridObjectsMaterials()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AddMaterialsFade(materialMap.Where((e, i) => i % 2 == 1).ToList(), 1f, 0);
        sequence.Play();
    }

    /// <summary>
    /// Calculates the position for the grid object at the given index.
    /// </summary>
    private Vector3 GetGridObjectPosition(int index, int gridSize, float objSize)
    {
        float space = spaceBetweenObjects + objSize;
        return new Vector3(index % gridSize * space, - (index / gridSize) * space, 0);
    }

    /// <summary>
    /// Sorts the objects for the grid arrangement. The objects are ordered by their
    /// distance from the most left and upper corner.
    /// </summary>
    private List<GameObject> SortForGrid(List<GameObject> objects)
    {
        Vector2 min = new Vector2(Mathf.Infinity, Mathf.NegativeInfinity);
        foreach (GameObject obj in objects)
            min = new Vector2(Mathf.Min(obj.transform.position.x), Mathf.Max(obj.transform.position.y));
        return objects.OrderBy(o => Vector2.Distance(o.transform.position,min)).ToList();
    }
    
    /// <summary>
    /// Updates the active state of all possible grid objects and destroys objects which do not
    /// exist in the real environment anymore. Is called by a Message from the TaskController.
    /// </summary>
    public void SyncObjects()
    {
        if (objMap == null)
            return;
        
        Dictionary<GameObject,GameObject> newTaskObjectMap = new Dictionary<GameObject, GameObject>();
        foreach (KeyValuePair<GameObject, GameObject> pair in objMap)
        {
            if (pair.Key == null)
            {
                Destroy(pair.Value);
                continue;
            }
            
            pair.Value.gameObject.SetActive(pair.Key.gameObject.activeSelf);
            newTaskObjectMap.Add(pair.Key,pair.Value);
        }
        objMap = newTaskObjectMap;
    }

    public override void TaskSolved(Vector3 targetPosition)
    {
        TaskController.Instance.TaskSolved(lastSelectedObjPos);
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
    }
}
