using System;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class HandsInitializer : MonoBehaviour
{
    public GameObject leftHandPrefab;
    public GameObject rightHandPrefab;

    private GameObject oldLeftHand;
    private GameObject oldRightHand;
    private Hand newLeftHand;
    private Hand newRightHand;
    private Player player;

    private bool applicationQuitting = false;
    
    void OnEnable()
    {
        Transform steamVRObjects = GameObject.Find("SteamVRObjects").transform;
        player = GameObject.Find("Player").GetComponent<Player>();
        
        oldLeftHand = player.leftHand.gameObject;
        oldRightHand = player.rightHand.gameObject;
        
        oldLeftHand.SetActive(false);
        oldRightHand.SetActive(false);

        newLeftHand = Instantiate(leftHandPrefab, steamVRObjects).GetComponent<Hand>();
        newLeftHand.name = gameObject.name + " Left Hand";
        newRightHand = Instantiate(rightHandPrefab, steamVRObjects).GetComponent<Hand>();
        newRightHand.name = gameObject.name + " Right Hand";

        newLeftHand.otherHand = newRightHand;
        newRightHand.otherHand = newLeftHand;

        player.hands[0] = newLeftHand;
        player.hands[1] = newRightHand;
    }

    void OnDisable()
    {
        if (applicationQuitting)
            return;
        
        Destroy(newLeftHand.gameObject);
        Destroy(newRightHand.gameObject);
        
        oldLeftHand.SetActive(true);
        oldRightHand.SetActive(true);
        
        player.hands[0] = oldLeftHand.GetComponent<Hand>();
        player.hands[1] = oldRightHand.GetComponent<Hand>();
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
    }
}
