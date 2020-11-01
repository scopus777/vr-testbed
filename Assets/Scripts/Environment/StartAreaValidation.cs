using TMPro;
using UnityEngine;
using Valve.VR.InteractionSystem;

/// <summary>
/// Shows the starting area room if the user is not inside the defined starting area.
/// The starting area room hints the user to return back to the starting position if
/// he is to far away from the starting position.
/// </summary>
public class StartAreaValidation : MonoBehaviour
{
    public Material[] targetAreaMaterials;
    public TMP_Text[] wallTexts;
    public Player player;
    public Transform startAreaCylinder;
    public float fadeDistance = 0.5f;

    private bool needsUpdate = true;
    private readonly int emission = Shader.PropertyToID("_EmissionColor");
    private readonly int color = Shader.PropertyToID("_Color");
    
    void Update()
    {
        // calculate distance between the user position and the starting position in x and z plane
        Vector3 planePlayerPosition = player.feetPositionGuess;
        planePlayerPosition.y = 0;
        Vector2 planeTargetAreaPosition = startAreaCylinder.transform.position;
        planeTargetAreaPosition.y = 0;
        float distance = Vector3.Distance(planePlayerPosition, planeTargetAreaPosition);
        
        // determine radius of the start area cylinder
        float radius = startAreaCylinder.transform.localScale.x / 2;
        
        // fade between normal environment and the starting area room if the user is not inside the starting area
        if (distance > radius)
        {
            float alpha = Mathf.Clamp((distance - radius) / fadeDistance, 0, 1);
            SetMaterialsAlpha(alpha);
            TaskController.Instance.SetCurrentGameObjectsActive(false);
            needsUpdate = true;
        }

        // ensure that the alpha values are updated once after the user leaves the fade area
        if ((distance >= radius + fadeDistance || distance <= radius) && needsUpdate)
        {
            if (distance >= radius + fadeDistance)
            {
                SetMaterialsAlpha(1);
            }
            else if (distance <= radius)
            {
                SetMaterialsAlpha(0);
                TaskController.Instance.SetCurrentGameObjectsActive(true);
            }

            needsUpdate = false;
        }

    }

    /// <summary>
    /// Sets the alpha values of the materials and the text objects.
    /// </summary>
    public void SetMaterialsAlpha(float alpha)
    {
        foreach (Material mat in targetAreaMaterials)
        {
            // rendering mode needs to be set to avoid disappearing objects if the rendering mode is always fade
            mat.SetMaterialRenderingMode(alpha < 1
                ? MaterialExtension.BlendMode.Fade
                : MaterialExtension.BlendMode.Opaque);
            Color c = mat.GetColor(color);
            mat.SetColor(color, new Color(c.r, c.g, c.b, alpha));
            c = mat.GetColor(emission);
            mat.SetColor(emission, new Color(c.r, c.g, c.b, alpha));
        }

        foreach (TMP_Text text in wallTexts)
            text.alpha = alpha;
    }
}
