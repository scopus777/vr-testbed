using UnityEngine;
using Valve.VR.InteractionSystem;

public class HideController : MonoBehaviour
{
    public void OnHandInitialized()
    {
        Hand hand = GetComponent<Hand>();
        hand.HideController();
        hand.mainRenderModel.onControllerLoaded += () => hand.HideController();
    }
}
