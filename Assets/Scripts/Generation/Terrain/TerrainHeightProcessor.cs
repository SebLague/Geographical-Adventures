using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace TerrainGeneration
{
	public class TerrainHeightProcessor : MonoBehaviour
	{

		[SerializeField] TerrainHeightSettings heightSettings;
		[SerializeField] ComputeShader heightMapCompute;
		[SerializeField] Texture2D heightMap;

		public RenderTexture processedHeightMap { get; private set; }

		public RenderTexture ProcessHeightMap()
		{
			const int worldHeightsKernel = 0;

			GraphicsFormat format = GraphicsFormat.R16_UNorm;
			processedHeightMap = ComputeHelper.CreateRenderTexture(heightMap.width, heightMap.height, FilterMode.Bilinear, format, "World Heights", useMipMaps: true);
			heightMapCompute.SetTexture(worldHeightsKernel, "RawHeightMap", heightMap);
			heightMapCompute.SetTexture(worldHeightsKernel, "WorldHeightMap", processedHeightMap);
			heightMapCompute.SetInts("size", heightMap.width, heightMap.height);
			ComputeHelper.Dispatch(heightMapCompute, processedHeightMap, kernelIndex: worldHeightsKernel);
			processedHeightMap.GenerateMips();
			return processedHeightMap;
		}

		public void Release()
		{
			ComputeHelper.Release(processedHeightMap);
			Resources.UnloadAsset(heightMap);
			heightMapCompute = null;
		}

		void OnDestroy()
		{
			Release();
		}
	}
}