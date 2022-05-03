using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountryHighlighting : MonoBehaviour
{
	public ComputeShader compute;
	public Transform player;
	public float fadeInDuration = 0.5f;
	public float fadeOutDuration = 0.5f;
	public float lookaheadDst = 5;
	ComputeBuffer countryHighlightsBuffer;
	int numCountries;
	public Texture2D countryIndexMap;
	bool initialized;


	public void Init(int numCountries)
	{
		countryHighlightsBuffer = ComputeHelper.CreateStructuredBuffer<float>(numCountries);
		BindConstantData();
		EditorOnlyInit();
		initialized = true;
	}

	void Update()
	{
		if (!initialized)
		{
			return;
		}

		Vector2 playerTexCoord = GeoMaths.PointToCoordinate((player.position + player.forward * lookaheadDst).normalized).ToUV();

		compute.SetVector("playerTexCoord", playerTexCoord);
		compute.SetFloat("deltaTime", Time.deltaTime);
		compute.SetFloat("fadeInSpeed", 1f / Mathf.Max(0.001f, fadeInDuration));
		compute.SetFloat("fadeOutSpeed", 1f / Mathf.Max(0.001f, fadeOutDuration));

		ComputeHelper.Dispatch(compute, countryHighlightsBuffer.count);
	}

	void BindConstantData()
	{
		compute.SetBuffer(0, "CountryHighlights", countryHighlightsBuffer);
		compute.SetTexture(0, "CountryIndexMap", countryIndexMap);
		compute.SetInt("width", countryIndexMap.width);
		compute.SetInt("height", countryIndexMap.height);
		compute.SetInt("numCountries", countryHighlightsBuffer.count);
	}

	public ComputeBuffer CountryHighlightsBuffer
	{
		get
		{
			return countryHighlightsBuffer;
		}
	}

	void OnDestroy()
	{
		ComputeHelper.Release(countryHighlightsBuffer);
	}

	void EditorOnlyInit()
	{
#if UNITY_EDITOR
		EditorShaderHelper.onRebindRequired += BindConstantData;
#endif
	}

}
