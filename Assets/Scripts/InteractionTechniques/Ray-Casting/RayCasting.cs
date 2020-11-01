using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class RayCasting : InteractionTechnique
{
    // Variables for Ray-Casting
    private LineRenderer renderedRay;
    private RaycastHit hit;

    protected override void Start()
    {
        base.Start();
        
        // find and activate ray on primary hand
        renderedRay = primaryHand.transform.Find("Ray").GetComponent<LineRenderer>();
        renderedRay.gameObject.SetActive(true);

        // ray is rendered in local space so the start position is always the position of the ray object
        renderedRay.SetPosition(0, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        // check if ray hits object
        Vector3 rayDirection = primaryHand.transform.forward;
        bool isHit = Physics.Raycast(renderedRay.transform.position, rayDirection, out hit, Mathf.Infinity);
        if (!isHit)
            return;

        // highlight object
        primaryHand.HoverLock(hit.collider.GetComponent<Interactable>());

        // Update length of rendered line to end at collision or after fixed length
        renderedRay.SetPosition(1,
            renderedRay.transform.InverseTransformDirection(rayDirection) * (primaryHand.currentAttachedObject
                ? Vector3.Distance(renderedRay.transform.position, primaryHand.currentAttachedObject.transform.position)
                : (isHit ? hit.distance : 10)));
    }
}