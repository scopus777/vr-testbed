using Valve.VR.InteractionSystem;

public static class HandExtension
{
    /// <summary>
    /// Hides the controller and the hover sphere.
    /// </summary>
    public static void FullyHideController(this Hand aParent)
    {
        aParent.HideController();
        aParent.hoverSphereTransform.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Shows the controller and the hover sphere.
    /// </summary>
    public static void FullyShowController(this Hand aParent)
    {
        aParent.ShowController();
        aParent.hoverSphereTransform.gameObject.SetActive(true);
    }
   
}
