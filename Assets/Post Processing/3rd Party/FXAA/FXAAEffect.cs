using System;
using UnityEngine;

// Original script by Jasper Flick (Catlike Coding)
// https://catlikecoding.com/unity/tutorials/advanced-rendering/fxaa/
// Minor modifications made

[CreateAssetMenu(menuName = "PostProcessing/FXAA")]
public class FXAAEffect : PostProcessingEffect
{

	const int luminancePass = 0;
	const int fxaaPass = 1;

	public enum LuminanceMode { Calculate, Alpha, Green }

	public LuminanceMode luminanceSource;

	[Range(0.0312f, 0.0833f)]
	public float contrastThreshold = 0.0312f;

	[Range(0.063f, 0.333f)]
	public float relativeThreshold = 0.063f;

	[Range(0f, 1f)]
	public float subpixelBlending = 1f;

	public bool lowQuality;

	public bool gammaBlending;


	protected override void RenderEffectToTarget(RenderTexture source, RenderTexture destination)
	{

		material.SetFloat("_ContrastThreshold", contrastThreshold);
		material.SetFloat("_RelativeThreshold", relativeThreshold);
		material.SetFloat("_SubpixelBlending", subpixelBlending);

		if (lowQuality)
		{
			material.EnableKeyword("LOW_QUALITY");
		}
		else
		{
			material.DisableKeyword("LOW_QUALITY");
		}

		if (gammaBlending)
		{
			material.EnableKeyword("GAMMA_BLENDING");
		}
		else
		{
			material.DisableKeyword("GAMMA_BLENDING");
		}

		if (luminanceSource == LuminanceMode.Calculate)
		{
			material.DisableKeyword("LUMINANCE_GREEN");
			RenderTexture luminanceTex = RenderTexture.GetTemporary(
				source.width, source.height, 0, source.format
			);
			Graphics.Blit(source, luminanceTex, material, luminancePass);
			Graphics.Blit(luminanceTex, destination, material, fxaaPass);
			RenderTexture.ReleaseTemporary(luminanceTex);
		}
		else
		{
			if (luminanceSource == LuminanceMode.Green)
			{
				material.EnableKeyword("LUMINANCE_GREEN");
			}
			else
			{
				material.DisableKeyword("LUMINANCE_GREEN");
			}
			Graphics.Blit(source, destination, material, fxaaPass);
		}
	}
}