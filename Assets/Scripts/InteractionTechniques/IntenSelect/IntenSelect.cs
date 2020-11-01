using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class IntenSelect : InteractionTechnique
{
    private IntenSelectCone cone;
    private BezierLines bezierRay;
    private Interactable targetInteractable;
    private Dictionary<Interactable, float> scoreMap;

    // needs to be changed of the mesh of the cone changes
    private float coneAngle = 7.5f;
    private const float k = 4f / 5f;
    private const float c_s = 0.9f;
    private const float c_g = 0.9f;

    protected override void Start()
    {
        primaryHand = TaskController.Instance.GetPrimaryHand();
        scoreMap = new Dictionary<Interactable, float>();
        cone = primaryHand.GetComponentInChildren<IntenSelectCone>();
        bezierRay = primaryHand.GetComponentInChildren<BezierLines>();
        
        pickupAction.AddOnStateDownListener(PickupObject, primaryHand.handType);
        
        SetNewHandPositionsAndRotations();
    }

    void Update()
    {
        Interactable tmpInteractable = null;

        if (cone.active)
        {
            // calculate the total score for all interactables in the scene and highlight the object with the highest score
            float currentMaxScore = 0;
            foreach (var go in TaskController.Instance.currentGameObjects)
            {
                Interactable interactable = go.GetComponent<Interactable>();
                if (!interactable)
                    continue;

                if (scoreMap.TryGetValue(interactable, out float score))
                {
                    if (score > 0.01f && score > currentMaxScore)
                    {
                        if (!interactable.isHovering)
                            interactable.SendMessage("OnHandHoverBegin", primaryHand,
                                SendMessageOptions.DontRequireReceiver);
                        tmpInteractable = interactable;
                        currentMaxScore = score;
                    }
                }
            }
        }

        // deselect all other objects
        foreach (var go in TaskController.Instance.currentGameObjects)
        {
            Interactable interactable = go.GetComponent<Interactable>();
            if (!interactable)
                continue;

            if (tmpInteractable == null || tmpInteractable != interactable)
                if (interactable.isHovering)
                    interactable.SendMessage("OnHandHoverEnd", primaryHand, SendMessageOptions.DontRequireReceiver);
        }

        targetInteractable = tmpInteractable;
        DrawBezierRay();
    }

    void FixedUpdate()
    {
        updateTotalContributionScore();
    }

    protected override void PickupObject(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        if (targetInteractable)
        {
            targetInteractable.SendMessage("OnHandHoverEnd", primaryHand, SendMessageOptions.DontRequireReceiver);
            targetInteractable.GetComponent<Takeable>().takeObject(primaryHand);
        }
        else
        {
            base.PickupObject(fromaction, fromsource);
        }
    }

    /// <summary>
    /// Called in TaskController when task objects are started to faded out.
    /// </summary>
    private void FadeOutAndRemoveTaskObjectsStart()
    {
        if (cone)
            cone.active = false;
    }

    /// <summary>
    /// Called in TaskController when task objects are finished to faded out.
    /// </summary>
    private void FadeOutAndRemoveTaskObjectsEnd()
    {
        if (cone)
            cone.active = true;
    }

    private void updateTotalContributionScore()
    {
        Dictionary<Interactable, float> newScoreMap = new Dictionary<Interactable, float>();

        foreach (var go in TaskController.Instance.currentGameObjects)
        {
            Interactable interactable = go.GetComponent<Interactable>();
            if (!interactable)
                continue;

            if (!scoreMap.ContainsKey(interactable))
                newScoreMap.Add(interactable, getCurrentScore(interactable));
            else
                newScoreMap.Add(interactable, scoreMap[interactable] * c_s + getCurrentScore(interactable) * c_g);
        }

        scoreMap = newScoreMap;
    }

    /// <summary>
    /// Calculates the current score for an interactable. 
    /// </summary>
    private float getCurrentScore(Interactable interactable)
    {
        if (!cone.selectableInteractables.Contains(interactable))
            return 0;
        
        // if only one object is selectable than the score can be set to non zero to force highlighting of this object
        if (cone.selectableInteractables.Count == 1)
            return 1;
        
        Vector3 localPos = cone.transform.InverseTransformPoint(interactable.transform.position);
        float d_perp = Mathf.Sqrt(Mathf.Pow(localPos.x, 2) + Mathf.Pow(localPos.y, 2));
        float alpha = Mathf.Atan(d_perp / Mathf.Pow(localPos.z, k)) * (180 / Mathf.PI);
        float s_contrib = 1 - alpha / coneAngle;
        return Mathf.Max(0,s_contrib);
    }

    /// <summary>
    /// Draws the bezier ray.
    /// </summary>
    private void DrawBezierRay()
    {
        bezierRay.RemoveCurves();

        if (targetInteractable == null)
            return;

        Vector3 pointOnLine = NearestPointOnLine(primaryHand.transform.position, primaryHand.transform.forward,
            targetInteractable.transform.position);
        Vector3 dir = targetInteractable.transform.position - pointOnLine;
        float dist1 = dir.magnitude;
        float dist2 = Vector3.Distance(primaryHand.transform.position, pointOnLine);

        Vector3[] bezierPoints = new Vector3[4];
        bezierPoints[0] = primaryHand.transform.position;
        bezierPoints[1] = primaryHand.transform.position + dist2 * (1f / 3f) * primaryHand.transform.forward.normalized;
        bezierPoints[2] = primaryHand.transform.position + dist1 * (2f / 3f) * dir.normalized +
                          dist2 * (2f / 3f) * primaryHand.transform.forward.normalized;
        bezierPoints[3] = targetInteractable.transform.position;

        bezierRay.AddCurve(bezierPoints, Color.red, Color.red, 0.005f, 0.0025f);

    }

    /// <summary>
    /// Returns the nearest point on a line for a given point.
    /// </summary>
    private Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        lineDir.Normalize();
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
    }

    protected override void OnDisable()
    {
        pickupAction.RemoveOnStateDownListener(PickupObject, primaryHand.handType);
    }
}
