using UnityEngine;
using TerrainGeneration;

public class WorldMeshGenerator : MonoBehaviour
{

	public TerrainHeightSettings heightSettings;
	public TextAsset terrainFile;

	public Material material;
	public Texture2D[] colourTiles;
	public Texture2D[] normalTiles;

	public CountryHighlighting countryHighlighter;
	public Light sun;
	Bounds[] allMeshBounds;
	Tile[] tiles;
	bool initialized;

	public void Init()
	{
		LoadTerrain();
		BindMaterialData();

		EditorShaderHelper.onRebindRequired += BindMaterialData;
		initialized = true;
	}

	void Update()
	{
		if (!initialized)
		{
			return;
		}
		foreach (Tile tile in tiles)
		{
			tile.UpdateRealtimeProperties(sun);
		}
	}

	void LoadTerrain()
	{
		// Create tiles
		tiles = new Tile[8];

		for (int y = 0; y < 2; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				int i = y * 4 + x;
				int i2 = (1 - y) * 4 + x;
				Vector2 tileOffset = new Vector2(x, y);
				tiles[i] = new Tile(material, colourTiles[i2], normalTiles[i2], tileOffset);
			}
		}

		FaceData[] faces = TerrainReader.LoadFromFile(terrainFile);
		allMeshBounds = new Bounds[faces.Length];

		// Create all faces
		for (int i = 0; i < faces.Length; i++)
		{
			// Create mesh holder
			Mesh mesh = faces[i].CreateMesh(heightSettings.worldRadius, heightSettings.heightMultiplier);
			var renderObject = MeshHelper.CreateRendererObject("Terrain Mesh " + i, mesh: mesh, parent: transform);

			allMeshBounds[i] = renderObject.filter.mesh.bounds;

			Vector3 centre = renderObject.filter.mesh.bounds.center.normalized;
			Vector2 uv = CoordinateSystem.PointToCoordinate(centre).ToUV();
			int tileX = (int)(4 * uv.x);
			int tileY = (int)(2 * uv.y);
			int tileIndex = tileY * 4 + tileX;
			renderObject.renderer.sharedMaterial = tiles[tileIndex].materialInstance;
		}
	}

	// Called once all generation has been completed, and so it safe to release any textures (etc.)
	// that other generators may have been using.
	public void OnGenerationStageFinished()
	{
		//	Resources.UnloadAsset(heightMap);
		// Maybe unnecessary (?)
		Resources.UnloadUnusedAssets();
	}

	void BindMaterialData()
	{
		foreach (var tile in tiles)
		{
			tile.UpdateMaterialData();
			tile.materialInstance.SetBuffer("CountryHighlights", countryHighlighter.CountryHighlightsBuffer);
		}
	}


	// (Editor only) Called from editor script when material is changed during play mode
	public void OnMaterialUpdated()
	{
		// Copy properties from original material to all instances to allow for easy tweaking of properties in the editor
		foreach (var tile in tiles)
		{
			if (Application.isEditor)
			{
				tile.materialInstance.CopyPropertiesFromMaterial(material);
			}
		}

		// Rebind unique data
		BindMaterialData();
	}

	public Bounds[] GetAllBounds()
	{
		return allMeshBounds;
	}

	public class Tile
	{
		public Material materialInstance;
		public Texture2D colourMap;
		public Texture2D normalMap;
		public Vector2 textureOffset;

		public Tile(Material material, Texture2D colourMap, Texture2D normalMap, Vector2 tileIndex)
		{
			materialInstance = new Material(material);
			this.colourMap = colourMap;
			this.normalMap = normalMap;
			this.textureOffset = tileIndex;
		}

		public void UpdateMaterialData()
		{
			materialInstance.SetTexture("ColourMap", colourMap);
			materialInstance.SetTexture("NormalMap", normalMap);
			materialInstance.SetVector("tileTexCoordOffset", textureOffset);
		}

		public void UpdateRealtimeProperties(Light sun)
		{
			materialInstance.SetVector(ShaderPropertyID.dirToSun, -sun.transform.forward);
			materialInstance.SetFloat(ShaderPropertyID.shadowStrength, sun.shadowStrength);
		}

		public class ShaderPropertyID
		{
			public static int dirToSun = Shader.PropertyToID("dirToSun");
			public static int shadowStrength = Shader.PropertyToID("shadowStrength");
		}
	}

}
