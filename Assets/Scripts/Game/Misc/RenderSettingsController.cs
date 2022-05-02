using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderSettingsController : MonoBehaviour
{

	[Header("Culling Settings")]
	const float minCullDst = 0.01f;
	public float maxCameraCullDst;
	public float maxLightShadowCullDst;
	public LayerOverride[] layerOverrides;

	[Header("Shadow Settings")]
	public ShadowResolution shadowResolution = ShadowResolution.VeryHigh;
	public float shadowDrawDistance;


	[Header("References")]
	public Light mainLight;
	public Camera mainCamera;

	void Awake()
	{
		ApplySettings();
	}

	void ApplySettings()
	{
		ApplyCullingValues();
		ApplyShadowSettings();
	}

	void ApplyCullingValues()
	{
		const int numLayers = 32;
		float[] cameraCullDstPerLayer = new float[numLayers];
		float[] lightCullDstPerLayer = new float[numLayers];

		// Initialize layers to max values
		for (int i = 0; i < numLayers; i++)
		{
			cameraCullDstPerLayer[i] = maxCameraCullDst;
			lightCullDstPerLayer[i] = maxLightShadowCullDst;
		}

		// Override specific layers
		for (int i = 0; i < layerOverrides.Length; i++)
		{
			LayerOverride layerOverride = layerOverrides[i];
			cameraCullDstPerLayer[layerOverride.layer] = layerOverride.cameraCullDst;
			lightCullDstPerLayer[layerOverride.layer] = layerOverride.shadowCullDst;
		}

		mainCamera.farClipPlane = maxCameraCullDst;
		mainCamera.layerCullDistances = cameraCullDstPerLayer;
		mainLight.layerShadowCullDistances = lightCullDstPerLayer;
	}

	void ApplyShadowSettings()
	{
		QualitySettings.shadowResolution = shadowResolution;
		QualitySettings.shadowDistance = shadowDrawDistance;
	}


	void OnValidate()
	{
		EnforceCorrectValues();
		ApplySettings();
	}

	void EnforceCorrectValues()
	{
		if (layerOverrides != null)
		{
			for (int i = 0; i < layerOverrides.Length; i++)
			{
				layerOverrides[i].EnforceCorrectValues();

				maxCameraCullDst = Mathf.Max(maxCameraCullDst, layerOverrides[i].cameraCullDst);
				maxLightShadowCullDst = Mathf.Max(maxLightShadowCullDst, layerOverrides[i].shadowCullDst);
			}
		}

		maxCameraCullDst = Mathf.Max(maxCameraCullDst, minCullDst);
		maxLightShadowCullDst = Mathf.Max(maxLightShadowCullDst, minCullDst);
		maxLightShadowCullDst = Mathf.Min(maxLightShadowCullDst, maxCameraCullDst);
	}

	[System.Serializable]
	public struct LayerOverride
	{
		[NaughtyAttributes.Layer]
		public int layer;
		public float cameraCullDst;
		public float shadowCullDst;

		public void EnforceCorrectValues()
		{
			// Value of zero is interpreted as 'use max value' which is a bit confusing, so enforce value to be above zero
			cameraCullDst = Mathf.Max(cameraCullDst, minCullDst);
			shadowCullDst = Mathf.Max(shadowCullDst, minCullDst);
			// Shadow culling distance cannot be greater than the camera cull distance
			shadowCullDst = Mathf.Min(shadowCullDst, cameraCullDst);
		}
	}

}
