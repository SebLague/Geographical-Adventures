using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

namespace TerrainGeneration
{
	public class MeshLoader : MonoBehaviour
	{

		public TextAsset loadFile;
		public Material mat;
		public bool useStaticBatching;
		public bool loadOnStart;
		public bool disableLoading;

		void Start()
		{
			if (loadOnStart)
			{
				Load(loadFile, mat, transform, useStaticBatching);
			}
		}

		public LoadInfo Load()
		{
			if (disableLoading) {
				return default;
			}
			return Load(loadFile, mat, transform, useStaticBatching, gameObject.layer);
		}

		public static LoadInfo Load(TextAsset loadFile, Material material, Transform parent, bool useStaticBatching, int layer = 0)
		{
			
			var sw = System.Diagnostics.Stopwatch.StartNew();
			LoadInfo info = new LoadInfo();

			SimpleMeshData[] meshData = MeshSerializer.BytesToMeshes(loadFile.bytes);

			GameObject[] allObjects = new GameObject[meshData.Length];

			for (int i = 0; i < meshData.Length; i++)
			{
				var renderObject = MeshHelper.CreateRendererObject(meshData[i].name, meshData[i], material, parent: parent, layer: layer);

				allObjects[i] = renderObject.gameObject;
				if (useStaticBatching)
				{
					allObjects[i].gameObject.isStatic = true;
				}
				info.vertexCount += meshData[i].vertices.Length;
				info.numMeshes++;
			}

			if (useStaticBatching)
			{
				StaticBatchingUtility.Combine(allObjects, parent.gameObject);
			}

			info.loadDuration = sw.ElapsedMilliseconds;

			return info;
		}

		public struct LoadInfo
		{
			public int vertexCount;
			public int numMeshes;
			public long loadDuration;
		}
	}

}