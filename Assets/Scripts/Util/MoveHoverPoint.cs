using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class MoveHoverPoint : MonoBehaviour
{
    public Vector3 ViveProHoverPointPosition;
    public Vector3 ViveCosmosHoverPointPosition;
    
    private SteamVR_Events.Action deviceConnectedAction;
    
    void Awake()
    {
        deviceConnectedAction = SteamVR_Events.RenderModelLoadedAction(Action);
    }
    
    private void Action(SteamVR_RenderModel arg0, bool arg1)
    {
        InteractionTechnique interactionTechnique = TaskController.Instance.GetInteractionTechnique();
        if (interactionTechnique && !interactionTechnique.moveHoverPoint)
            return;

        Hand hand = arg0.GetComponentInParent<Hand>();
        if (!hand)
            return;

        Transform hoverPoint = hand.transform.Find("HoverPoint");
        if (!hoverPoint)
            return;
            
        if (arg0.renderModelName.Contains("vive_cosmos"))
            hoverPoint.localPosition = ViveCosmosHoverPointPosition;
        else if (arg0.renderModelName.Contains("controller_vive"))
            hoverPoint.localPosition = ViveProHoverPointPosition;
    }

    private void OnEnable()
    {
        deviceConnectedAction.enabled = true;
    }
    
    private void OnDisable()
    {
        deviceConnectedAction.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
