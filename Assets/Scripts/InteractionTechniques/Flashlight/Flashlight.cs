using UnityEngine;
using Valve.VR.InteractionSystem;

public class Flashlight : InteractionTechnique
{
    public FlashlightSelectionCone selectionCone;

    protected override void Start()
    {
        base.Start();
        selectionCone.transform.parent.SetParent(primaryHand.transform);
        selectionCone.transform.parent.localPosition = Vector3.zero;
        selectionCone.transform.parent.rotation = primaryHand.transform.rotation;
    }

    private void Update() 
    {
        Interactable currentInteractable = null;
        float currentDistance = Mathf.Infinity;
        
        // Highlight the object which is closest to the center of the cone
        foreach (Interactable interactable in selectionCone.selectableInteractables)
        {
            if (interactable)
            {
                float distance = HelperUtil.DistancePointLine(interactable.transform.position, primaryHand.transform.position,
                    selectionCone.transform.position);
                if (distance < currentDistance)
                {
                    currentDistance = distance;
                    currentInteractable = interactable;
                }
            }
        }
        primaryHand.HoverLock(currentInteractable);
    }
}
