using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class FlashlightSelectionCone : MonoBehaviour
{

    public bool active { get; set; }
    public List<Interactable> selectableInteractables { get; set; }

    private void Start()
    {
        active = true;
        selectableInteractables = new List<Interactable>();
    }

    private void Update()
    {
        List<Interactable> newInteractables = new List<Interactable>();   
        foreach (Interactable interactable in selectableInteractables)
            if (interactable)
                newInteractables.Add(interactable);
        selectableInteractables = newInteractables;
    }

    /// <summary>
    /// Called when an objects moves into the selection cone
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!active)
            return;

        Interactable interactable = other.gameObject.GetComponent<Interactable>();
        if (!interactable)
            return;

        selectableInteractables.Add(other.gameObject.GetComponent<Interactable>());
    }

    /// <summary>
    /// Called when an objects leaves the selection cone
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!active)
            return;

        Interactable interactable = other.GetComponent<Interactable>();
        if (!interactable)
            return;

        OnTriggerDeselect(interactable, selectableInteractables);

    }

    private void OnTriggerDeselect(Interactable interactable, List<Interactable> interactables)
    {
        if (!interactables.Contains(interactable))
            return;

        interactables.Remove(interactable);
    }
}