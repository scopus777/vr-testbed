using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class HeadBasedSelection : InteractionTechnique
{
    public Transform head;
    public float cursorSizeMultiplier = 0.5f;
    public bool buttonSelection;
    public float selectionTime;
    public Image hoverCircle;
    public float hoverCircleSizeMultiplier = 0.002f;

    // Variables for Ray-Casting
    private RaycastHit hit;
    private Transform selectionCursor;
    private float startHitTime;
    private Interactable lastInteractable;
    private bool isTaken;

    protected override void Start()
    {
        base.Start();
        selectionCursor = transform.Find("Cursor");
    }

    // Update is called once per frame
    void Update()
    {
        // check if ray hits object
        bool isHit = Physics.Raycast(head.position, head.forward, out hit, Mathf.Infinity);

        // highlight object
        Interactable interactable = hit.collider.GetComponent<Interactable>();
        primaryHand.HoverLock(hit.collider.GetComponent<Interactable>());

        if (isHit)
        {
            selectionCursor.gameObject.SetActive(true);
            selectionCursor.position = hit.point;
            selectionCursor.localScale =
                cursorSizeMultiplier * Mathf.Sqrt(Vector3.Distance(hit.point, head.position)) * Vector3.one;
        }
        else
        {
            selectionCursor.gameObject.SetActive(false);
        }

        if (buttonSelection)
            return;

        if (isHit && interactable && interactable.Equals(lastInteractable) && !isTaken)
        {
            Transform parentCanvas = hoverCircle.transform.parent;
            parentCanvas.gameObject.SetActive(true);
            float timeOnObject = Time.realtimeSinceStartup - startHitTime;
            hoverCircle.fillAmount = Mathf.Clamp01(timeOnObject / selectionTime);
            parentCanvas.localScale =
                hoverCircleSizeMultiplier * Mathf.Sqrt(Vector3.Distance(hit.point, head.position)) * Vector3.one;

            Physics.Raycast(head.position, interactable.transform.position - head.position, out RaycastHit newHit,
                Mathf.Infinity);
            parentCanvas.position = newHit.point;
            parentCanvas.LookAt(head);

            if (timeOnObject >= selectionTime)
            {
                interactable.GetComponent<Takeable>().takeObject(primaryHand);
                isTaken = true;
            }
        }
        else
        {
            hoverCircle.transform.parent.gameObject.SetActive(false);
            startHitTime = Time.realtimeSinceStartup;
            isTaken = false;
        }

        lastInteractable = interactable;
    }
}