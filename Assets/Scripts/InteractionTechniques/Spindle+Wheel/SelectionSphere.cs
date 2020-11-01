using System;
using UnityEngine;
using Valve.VR.InteractionSystem;

/// <summary>
/// Saves the interactable this object is colliding with in the hoveringInteractable variable.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SelectionSphere : MonoBehaviour
{
    public Interactable hoveringInteractable { private set; get; }

    private void OnTriggerEnter(Collider other)
    {
        hoveringInteractable = other.gameObject.GetComponent<Interactable>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (hoveringInteractable != null)
        {
            Bounds bounds = hoveringInteractable.GetComponent<Collider>().bounds;
            if (!bounds.Contains(hoveringInteractable.transform.InverseTransformPoint(transform.position)))
                hoveringInteractable = null;
        }
    }
}
