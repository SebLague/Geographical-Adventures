using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

public class LodMeshLoader : MonoBehaviour
{
	public TextAsset meshFileHighRes;
	public TextAsset meshFileLowRes;

	public Material mat;
	public Material lowResMat;
	public bool useStaticBatching;
	public bool loadOnStart;
	public SimpleLodSystem lodSystem;


	void Start()
	{
		if (loadOnStart)
		{
			Load();
		}
	}

	public void Load()
	{
		MeshRenderer[] highResRenderers = CreateRenderers(meshFileHighRes, mat);
		MeshRenderer[] lowResRenderers = CreateRenderers(meshFileLowRes, lowResMat);

		Debug.Assert(highResRenderers.Length == lowResRenderers.Length, "Mismatch in number of high and low res meshes");

		for (int i = 0; i < highResRenderers.Length; i++)
		{
			lodSystem.AddLOD(highResRenderers[i], lowResRenderers[i]);
		}


	}

	MeshRenderer[] CreateRenderers(TextAsset loadFile, Material material)
	{
		SimpleMeshData[] meshData = MeshSerializer.BytesToMeshes(loadFile.bytes);
		MeshRenderer[] meshRenderers = new MeshRenderer[meshData.Length];
		GameObject[] allObjects = new GameObject[meshData.Length];


		for (int i = 0; i < meshRenderers.Length; i++)
		{
			var renderObject = MeshHelper.CreateRendererObject(meshData[i].name, meshData[i], material, parent: transform, gameObject.layer);

			meshRenderers[i] = renderObject.renderer;
			allObjects[i] = renderObject.gameObject;

			if (useStaticBatching)
			{
				meshRenderers[i].gameObject.isStatic = true;
			}
		}

		if (useStaticBatching)
		{
			StaticBatchingUtility.Combine(allObjects, gameObject);
		}

		return meshRenderers;
	}

}
