using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration;
using Seb.Meshing;

public class GlobeMapCreator : Generator
{
	[Header("Settings")]
	public int resolution = 100;
	public int oceanResolution = 50;
	public float minRaiseHeight;
	public float raiseHeightMultiplier;
	public float radius = 10;
	public float tinyAreaThreshold;

	[Header("References")]
	public Material countryMaterial;
	public Material oceanMaterial;
	public CountryLoader countryLoader;
	public TextAsset averageHeightFile;

	[Header("Save/Load")]
	public string countriesSaveFileName;
	public string oceanSaveFileName;
	public TextAsset loadFile;

	Vector3[] spherePoints;
	Coordinate[] spherePoints2D;

	Dictionary<string, float> averageCountryElevations;
	float maxElevation;
	SimpleMeshData[] allCountriesMeshData;
	SimpleMeshData oceanMeshData;

	public override void StartGenerating()
	{
		NotifyGenerationStarted();
		StartCoroutine(Generate());
	}

	IEnumerator Generate()
	{
		// Create ocean mesh
		oceanMeshData = IcoSphere.Generate(oceanResolution, radius);
		MeshHelper.CreateRendererObject("Ocean", oceanMeshData, oceanMaterial, transform);

		Country[] countries = countryLoader.GetCountries();
		allCountriesMeshData = new SimpleMeshData[countries.Length];

		spherePoints = IcoSphere.Generate(resolution).vertices;
		spherePoints2D = new Coordinate[spherePoints.Length];
		for (int i = 0; i < spherePoints.Length; i++)
		{
			spherePoints2D[i] = GeoMaths.PointToCoordinate(spherePoints[i]);
		}

		// Load average heights
		averageCountryElevations = new Dictionary<string, float>();
		maxElevation = 0;
		string[] entries = averageHeightFile.text.Split('\n');
		foreach (string entry in entries)
		{
			string[] data = entry.Split(',');
			float height = float.Parse(data[1]);
			averageCountryElevations.Add(data[0], height);
			maxElevation = Mathf.Max(maxElevation, height);
		}

		// Create country meshes
		for (int i = 0; i < countries.Length; i++)
		{
			SimpleMeshData countryMeshData = GenerateCountry(countries[i]);
			string countryName = countries[i].GetPreferredDisplayName();
			countryMeshData.name = countryName;
			MeshHelper.CreateRendererObject(countryName, countryMeshData, countryMaterial, transform);
			allCountriesMeshData[i] = countryMeshData;
			yield return null;
		}

		Debug.Log("Generation Complete");
		NotifyGenerationComplete();
	}
	public override void Save()
	{
		// Save countries
		foreach (var meshData in allCountriesMeshData)
		{
			meshData.Optimize();
		}

		byte[] bytes = MeshSerializer.MeshesToBytes(allCountriesMeshData);
		FileHelper.SaveBytesToFile(SavePath, countriesSaveFileName, bytes, log: true);

		// Save ocean
		oceanMeshData.Optimize();
		byte[] oceanBytes = MeshSerializer.MeshToBytes(oceanMeshData);
		FileHelper.SaveBytesToFile(SavePath, oceanSaveFileName, oceanBytes, log: true);
	}

	public override void Load()
	{
		SimpleMeshData[] meshes = MeshSerializer.BytesToMeshes(loadFile.bytes);
		for (int i = 0; i < meshes.Length; i++)
		{
			MeshHelper.CreateRendererObject(meshes[i].name, meshes[i], countryMaterial, transform);
		}
	}

	SimpleMeshData GenerateCountry(Country country)
	{
		SimpleMeshData countryMeshData = new SimpleMeshData(country.name);

		for (int i = 0; i < country.shape.polygons.Length; i++)
		{
			// Try get average height. Note: this data is from different source so some names might not match. (TODO: fix)
			float h = 0;
			bool a = averageCountryElevations.TryGetValue(country.name, out h);
			bool b = averageCountryElevations.TryGetValue(country.nameOfficial, out h);
			bool c = averageCountryElevations.TryGetValue(country.name_long, out h);
			h = h / maxElevation;
			float elevation = minRaiseHeight + h * raiseHeightMultiplier;
			SimpleMeshData polygonMeshData = GeneratePolygon(country.shape.polygons[i], elevation, country.name);
			if (polygonMeshData != null)
			{
				countryMeshData.Combine(polygonMeshData);
			}
		}

		countryMeshData.RecalculateNormals();
		return countryMeshData;
	}

	SimpleMeshData GeneratePolygon(Polygon polygon, float elevation, string countryName)
	{
		//DebugExtra.DrawPath(polygon.paths[0].GetPointsAsVector2(), false, Color.red, 1000);
		List<Coordinate> innerPoints = new List<Coordinate>();
		Vector2[] originalOutline = polygon.paths[0].GetPointsAsVector2(includeLastPoint: false);
		Bounds2D bounds2D = new Bounds2D(originalOutline);

		for (int i = 0; i < spherePoints2D.Length; i++)
		{
			Vector2 p = spherePoints2D[i].ToVector2();
			if (bounds2D.Contains(p))
			{
				if (Seb.Maths.PolygonContainsPoint(p, originalOutline))
				{
					innerPoints.Add(spherePoints2D[i]);
				}
			}
		}

		Polygon processedPolygon = PolygonProcessor.RemoveDuplicatesAndEdgePoints(polygon);
		(Polygon reprojectedPolygon, Coordinate[] reprojectedInnerPoints) = PolygonProcessor.Reproject(processedPolygon, innerPoints.ToArray());

		if (processedPolygon.paths[0].NumPoints > 3)
		{
			float area = Seb.Maths.PolygonArea(processedPolygon.Outline.GetPointsAsVector2());
			float elevationMultiplier = Mathf.Lerp(0.05f, 1, area / tinyAreaThreshold);
			elevation *= elevationMultiplier;

			int[] triangles = Triangulator.Triangulate(reprojectedPolygon, reprojectedInnerPoints, false);

			List<Vector3> vertices = new List<Vector3>();
			Vector3[] outlinePoints = SpherizePoints(processedPolygon.paths[0].points, radius + elevation);
			vertices.AddRange(outlinePoints);
			vertices.AddRange(SpherizePoints(innerPoints.ToArray(), radius + elevation));
			for (int i = 0; i < processedPolygon.NumHoles; i++)
			{
				vertices.AddRange(SpherizePoints(processedPolygon.Holes[i].points, radius + elevation));
			}

			// Create rim mesh
			SimpleMeshData rim = RimMeshGenerator.GenerateOnSphere(outlinePoints, elevation + 1);
			SimpleMeshData meshData = new SimpleMeshData(vertices.ToArray(), triangles);
			meshData.Combine(rim);

			return meshData;
		}
		return null;
	}

	Vector3[] SpherizePoints(Coordinate[] points, float radius)
	{

		Vector3[] pointsOnSphere = new Vector3[points.Length];
		for (int i = 0; i < pointsOnSphere.Length; i++)
		{
			pointsOnSphere[i] = GeoMaths.CoordinateToPoint(points[i], radius);
		}

		return pointsOnSphere;
	}

	protected override string SavePath
	{
		get
		{
			return FileHelper.MakePath("Assets", "Graphics", "Globe Map", "Meshes"); ;
		}
	}
}