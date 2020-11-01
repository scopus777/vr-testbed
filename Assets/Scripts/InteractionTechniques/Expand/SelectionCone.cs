using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class SelectionCone : MonoBehaviour
{
    [HideInInspector] public bool inGridPhase;
    [HideInInspector] public List<GameObject> selectableInteractables;
    [HideInInspector] public List<GameObject> interactablesInSelection;
    [HideInInspector] public bool active;

    private Hand primaryHand;
    private Mesh mesh;

    void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        selectableInteractables = new List<GameObject>();
        interactablesInSelection = new List<GameObject>();
        mesh = GetComponent<MeshFilter>().mesh;
    }

    void Update()
    {
        if (!active)
            return;

        // if in grid phase ensure that only one object can be selected
        List<GameObject> interactablesInSelectionNew = new List<GameObject>();
        foreach (GameObject interactable in selectableInteractables)
        {
            if (!interactable.activeSelf)
                continue;

            if (isInCone(interactable.transform.position))
            {
                interactablesInSelectionNew.Add(interactable);
                if (!interactable.GetComponent<Interactable>().isHovering)
                    interactable.SendMessage("OnHandHoverBegin", primaryHand, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (interactable.GetComponent<Interactable>().isHovering)
                    interactable.SendMessage("OnHandHoverEnd", primaryHand, SendMessageOptions.DontRequireReceiver);
            }
        }

        interactablesInSelection = interactablesInSelectionNew;

        if (inGridPhase)
            EnsureSingleSelection();
    }

    /// <summary>
    /// Checks whether the point is in the cone.
    /// </summary>
    private bool isInCone(Vector3 point)
    {
        return mesh.IsPointInside(transform.InverseTransformPoint(point));
    }

    /// <summary>
    /// Resets the cone.
    /// </summary>
    public void Reset()
    {
        DeselectAll();
        interactablesInSelection = new List<GameObject>();
        inGridPhase = false;
        active = true;
    }

    /// <summary>
    /// Deselects all objects.
    /// </summary>
    public void DeselectAll()
    {
        foreach (GameObject interactable in interactablesInSelection)
            if (interactable)
                interactable.SendMessage("OnHandHoverEnd", primaryHand, SendMessageOptions.DontRequireReceiver);
        interactablesInSelection = new List<GameObject>();
    }

    /// <summary>
    /// Ensures that only one object is highlighted and selectable in grid mode.
    /// </summary>
    private void EnsureSingleSelection()
    {
        float smallestDistance = Mathf.Infinity;
        GameObject closestInteractable = null;
        foreach (GameObject interactable in interactablesInSelection)
        {
            float distanceConeCenter = Vector3.Cross(transform.up, interactable.transform.position - transform.position)
                .magnitude;
            if (distanceConeCenter < smallestDistance)
            {
                smallestDistance = distanceConeCenter;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable == null)
            return;

        interactablesInSelection.Remove(closestInteractable);
        interactablesInSelection.Insert(0, closestInteractable);
        if (!closestInteractable.GetComponent<Interactable>().isHovering)
            closestInteractable.SendMessage("OnHandHoverBegin", primaryHand, SendMessageOptions.DontRequireReceiver);

        for (int i = 1; i < interactablesInSelection.Count; i++)
            if (interactablesInSelection[i].GetComponent<Interactable>().isHovering)
                interactablesInSelection[i]
                    .SendMessage("OnHandHoverEnd", primaryHand, SendMessageOptions.DontRequireReceiver);
    }
}
