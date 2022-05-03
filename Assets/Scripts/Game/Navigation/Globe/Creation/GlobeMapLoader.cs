using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

public class GlobeMapLoader : MonoBehaviour
{

	public bool autoLoadOnAwake;
	public TextAsset countriesLoadFile;
	public TextAsset oceanLoadFile;

	public Material countryMaterial;
	public Material oceanMaterial;

	public Transform holder;

	public bool hasLoaded { get; private set; }
	public GameObject oceanObject { get; private set; }
	public RenderObject[] countryObjects { get; private set; }

	public CountryColour[] countryColours;
	public bool updateColoursFromRenderer;

	void Awake()
	{
		if (autoLoadOnAwake)
		{
			Load();
		}
	}

	void Update()
	{
		if (updateColoursFromRenderer)
		{
			for (int i = 0; i < countryObjects.Length; i++)
			{
				countryColours[i].colour = countryObjects[i].material.color;
			}
		}

	}

	public void Load()
	{
		LoadCountries();

		// Load ocean
		SimpleMeshData oceanMesh = MeshSerializer.BytesToMesh(oceanLoadFile.bytes);
		var oceanRenderObject = MeshHelper.CreateRendererObject("Ocean", oceanMesh, oceanMaterial, holder, holder.gameObject.layer);
		oceanObject = oceanRenderObject.gameObject;
		AddCollider(oceanRenderObject);
		hasLoaded = true;
	}



	void LoadCountries()
	{

		// Load countries
		SimpleMeshData[] meshes = MeshSerializer.BytesToMeshes(countriesLoadFile.bytes);
		countryObjects = new RenderObject[meshes.Length];
		GameObject[] allObjects = new GameObject[meshes.Length];

		for (int i = 0; i < meshes.Length; i++)
		{

			Material materialInstance = new Material(countryMaterial);
			materialInstance.color = countryColours[i].colour;
			var countryRenderObject = MeshHelper.CreateRendererObject(meshes[i].name, meshes[i], materialInstance, holder, holder.gameObject.layer);

			AddCollider(countryRenderObject);
			countryObjects[i] = countryRenderObject;
			allObjects[i] = countryRenderObject.gameObject;
			allObjects[i].isStatic = true;
		}


		StaticBatchingUtility.Combine(allObjects, holder.gameObject);



	}

	void AddCollider(RenderObject renderObject)
	{
		// Add collider for detecting mouse over
		// Bake mesh during loading so doesn't have to do it when map is first enabled
		MeshCollider meshCollider = renderObject.gameObject.AddComponent<MeshCollider>();
		Physics.BakeMesh(renderObject.filter.mesh.GetInstanceID(), convex: false);
		meshCollider.sharedMesh = renderObject.filter.mesh;
	}


	[System.Serializable]
	public struct CountryColour
	{
		public string countryName;
		public Color colour;
	}

}

