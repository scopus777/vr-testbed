using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public static class TweenExtension 
{
    private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
    private static readonly int Color = Shader.PropertyToID("_Color");

    /// <summary>
    /// Fades an object in dependence to the given alpha value.
    /// The default DOFade method only fades the main color and ignores the emission color.
    /// </summary>
    public static Sequence AddMaterialsFade(this Sequence aParent, List<Material> materials, float alpha, float duration, bool join = false)
    {
        setRenderingMode(materials, MaterialExtension.BlendMode.Fade);
        
        if (join)
            aParent.Join(materials[0].DOFade(alpha,duration));
        else
            aParent.Append(materials[0].DOFade(alpha,duration));
        for (int i = 1; i < materials.Count; i++)
            aParent.Join(materials[i].DOFade(alpha, duration));
        
        // needed to avoid transparency conflicts (e.g. with the handle in the crank handle technique)
        if (alpha >= 1)
            aParent.OnComplete(() => setRenderingMode(materials, MaterialExtension.BlendMode.Opaque));

        return aParent;
    }

    private static void setRenderingMode(List<Material> materials, MaterialExtension.BlendMode blendMode)
    {
        foreach (var t in materials.Where(m => !m.shader.name.Equals("CustomStandard")))
            t.SetMaterialRenderingMode(blendMode);
    }
}
