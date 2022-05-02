using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class WorldLookup : MonoBehaviour
{
	public TerrainGeneration.TerrainHeightSettings heightSettings;
	public ComputeShader heightMapCompute;
	public ComputeShader lookupShader;
	public Texture2D countryIndices;

	// Small map containing normalized height values
	RenderTexture heightLookup;

	public void Init(RenderTexture heightMap)
	{
		GraphicsFormat format = GraphicsFormat.R8_UNorm;
		heightLookup = ComputeHelper.CreateRenderTexture(4096, 2048, FilterMode.Bilinear, format, "Height Lookup");
		Graphics.Blit(heightMap, heightLookup);
	}

	ComputeBuffer RunLookupCompute(Coordinate coordinate)
	{
		ComputeBuffer resultBuffer = ComputeHelper.CreateStructuredBuffer<float>(2);
		lookupShader.SetTexture(0, "HeightMap", heightLookup);
		lookupShader.SetTexture(0, "CountryIndices", countryIndices);
		lookupShader.SetBuffer(0, "Result", resultBuffer);
		lookupShader.SetVector("uv", coordinate.ToUV());
		ComputeHelper.Dispatch(lookupShader, 1);
		return resultBuffer;
	}

	public void GetTerrainInfoAsync(Coordinate coord, System.Action<TerrainInfo> callback)
	{
		if (SystemInfo.supportsAsyncGPUReadback)
		{
			ComputeBuffer resultBuffer = RunLookupCompute(coord);
			AsyncGPUReadback.Request(resultBuffer, (request) => AsyncRequestComplete(request, resultBuffer, callback));
		}
		else
		{
			callback.Invoke(GetTerrainInfoImmediate(coord));
		}
	}

	public void GetTerrainInfoAsync(Vector3 point, System.Action<TerrainInfo> callback)
	{
		Coordinate coord = GeoMaths.PointToCoordinate(point.normalized);
		GetTerrainInfoAsync(coord, callback);
	}

	public TerrainInfo GetTerrainInfoImmediate(Coordinate coordinate)
	{
		ComputeBuffer resultBuffer = RunLookupCompute(coordinate);
		float[] data = new float[2];
		resultBuffer.GetData(data);
		resultBuffer.Release();
		return CreateTerrainInfoFromData(data);
	}

	void AsyncRequestComplete(AsyncGPUReadbackRequest request, ComputeBuffer buffer, System.Action<TerrainInfo> callback)
	{
		if (Application.isPlaying && !request.hasError)
		{
			var info = CreateTerrainInfoFromData(request.GetData<float>().ToArray());
			callback?.Invoke(info);
		}

		ComputeHelper.Release(buffer);

	}

	TerrainInfo CreateTerrainInfoFromData(float[] data)
	{
		float heightT = data[0];
		float countryT = data[1];

		float worldHeight = heightSettings.worldRadius + heightT * heightSettings.heightMultiplier;
		int countryIndex = (int)(countryT * 255.0) - 1;
		TerrainInfo info = new TerrainInfo(worldHeight, countryIndex);
		return info;
	}


	void OnDestroy()
	{
		// Getting a warning when exiting playmode from menu scene after async loading game scene (but not activating).
		// This stops the warning. TODO: investigate
		if (RenderTexture.active == heightLookup)
		{
			RenderTexture.active = null;
		}

		ComputeHelper.Release(heightLookup);
	}
}

public struct TerrainInfo
{
	public readonly float height;
	public readonly int countryIndex;

	public TerrainInfo(float height, int countryIndex)
	{
		this.height = height;
		this.countryIndex = countryIndex;
	}

	public bool inOcean
	{
		get
		{
			return countryIndex < 0;
		}
	}

}
