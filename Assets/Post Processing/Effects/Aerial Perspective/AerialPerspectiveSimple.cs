using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PostProcessing/Aerial Perspective")]
public class AerialPerspectiveSimple : PostProcessingEffect
{

	public Vector2 depthMinMax;
	public float strength;
	public Color atmoCol;

	protected override void RenderEffectToTarget(RenderTexture source, RenderTexture target)
	{
		material.SetVector("depthMinMax", depthMinMax);
		material.SetColor("atmoCol", atmoCol);
		material.SetFloat("strength", strength);
		Graphics.Blit(source, target, material);
	}
}
