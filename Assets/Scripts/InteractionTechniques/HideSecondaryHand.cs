using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Hand))]
public class HideSecondaryHand : MonoBehaviour
{
    public List<GameObject> objectsToHide;
    private Hand hand;
    
    // Start is called before the first frame update
    void Start()
    {
        hand = GetComponent<Hand>();

        if (hand.handType == TaskController.Instance.GetPrimaryHand().handType)
            enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (hand.mainRenderModel != null)
        {
            hand.useHoverSphere = false;
            hand.mainRenderModel.gameObject.SetActive(false);
            objectsToHide.ForEach(o => o.SetActive(false));
            enabled = false;
        }
    }
}
