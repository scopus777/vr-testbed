using UnityEngine;

/// <summary>
/// Provides a function to change the rendering mode of a material.
///
/// Source: https://forum.unity-community.de/topic/10035-change-material-renderingmode-on-runtime/
/// </summary>
public static class MaterialExtension
{
	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,
		Transparent
	}

	public static void SetMaterialRenderingMode(this Material pMaterial, BlendMode pBlendMode)
	{
		switch (pBlendMode)
		{
			case BlendMode.Opaque:
				pMaterial.SetFloat("_Mode", 0);
				pMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
				pMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
				pMaterial.SetInt("_ZWrite", 1);
				pMaterial.DisableKeyword("_ALPHATEST_ON");
				pMaterial.DisableKeyword("_ALPHABLEND_ON");
				pMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				pMaterial.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				pMaterial.SetFloat("_Mode", 1);
				pMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
				pMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
				pMaterial.SetInt("_ZWrite", 1);
				pMaterial.EnableKeyword("_ALPHATEST_ON");
				pMaterial.DisableKeyword("_ALPHABLEND_ON");
				pMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				pMaterial.renderQueue = 2450;
				break;
			case BlendMode.Fade:
				pMaterial.SetFloat("_Mode", 2);
				pMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
				pMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				pMaterial.SetInt("_ZWrite", 0);
				pMaterial.DisableKeyword("_ALPHATEST_ON");
				pMaterial.EnableKeyword("_ALPHABLEND_ON");
				pMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				pMaterial.renderQueue = 3001;
				break;
			case BlendMode.Transparent:
				pMaterial.SetFloat("_Mode", 3);
				pMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
				pMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				pMaterial.SetInt("_ZWrite", 0);
				pMaterial.DisableKeyword("_ALPHATEST_ON");
				pMaterial.DisableKeyword("_ALPHABLEND_ON");
				pMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				pMaterial.renderQueue = 3001;
				break;
		}
	}

	public static void SetMainColorAlpha(this Material m, float alpha)
	{
		m.SetMaterialRenderingMode(alpha >= 1 ? BlendMode.Opaque : BlendMode.Fade);
		Color color = m.color;
		color.a = alpha;
		m.color = color;
	}
}