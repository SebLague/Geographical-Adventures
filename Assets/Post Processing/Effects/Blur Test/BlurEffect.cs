using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PostProcessing/Blur")]
public class BlurEffect : PostProcessingEffect
{

	[Range(0, 25)] public float blurRadius = 10;

	protected override void RenderEffectToTarget(RenderTexture source, RenderTexture destination)
	{
		if (blurRadius > 0)
		{
			RenderTexture temp = RenderTexture.GetTemporary(source.descriptor);
			material.SetFloat(ShaderProperties.blurRadiusID, blurRadius + 1);
			Graphics.Blit(source, temp, material, 0);
			Graphics.Blit(temp, destination, material, 1);
			RenderTexture.ReleaseTemporary(temp);
		}
		else
		{
			Graphics.Blit(source, destination);
		}
	}

	struct ShaderProperties
	{
		public static int blurRadiusID = Shader.PropertyToID("blurRadius");
	}

}
