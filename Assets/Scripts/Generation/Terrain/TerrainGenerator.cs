using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

namespace TerrainGeneration
{
	public class TerrainGenerator : Generator
	{

		[Header("Settings")]
		public float stepSize;
		public float errorThreshold;
		public float normalsStepSize;
		public float minHeight;

		public int minResolution;
		public int maxResolution;

		[Disabled] public int numSpherePointsMax;
		[Disabled] public int numSpherePointsMin;

		public enum MeshCombineMode { OneMeshPerPolygon, OneMeshPerCountry, MultipleMeshesPerCountry }
		public MeshCombineMode meshCombineMode;

		[Header("References")]

		public CountryData countryData;
		public ComputeShader vertexCompute;
		public ComputeShader meshNormalsCompute;
		public Material testMat;
		public PolygonProcessor polygonProcessor;
		public Coastline coastline;
		public TerrainGeneration.TerrainHeightSettings heightSettings;
		public Transform meshHolder;


		[Header("Save/Load Settings")]
		public string meshSaveFileName;
		public string outlinesSaveFileName;
		public TextAsset loadFile;

		Country[] countries;

		public TerrainGeneration.TerrainHeightProcessor heightProcessor;

		[Header("Debug info")]
		[Disabled] public int totalVertexCount;
		[Disabled] public int numMeshesBeforeCombining;
		[Disabled] public int numMeshesAfterCombining;

		ComputeBuffer spherePointsBuffer;
		ComputeBuffer gridSpherePointsBuffer;
		ComputeBuffer innerVertices2DBuffer;

		const int compute2DVerticesKernel = 0;
		const int assignVertexHeightsKernel = 1;

		// Generation result
		List<SimpleMeshData> allCombinedMeshes;
		List<PolygonMeshData> allPolygonMeshData;

		protected override void Start()
		{
			base.Start();
		}

		public override void StartGenerating()
		{
			NotifyGenerationStarted();

			countries = countryData.Countries;
			allPolygonMeshData = new List<PolygonMeshData>();
			allCombinedMeshes = new List<SimpleMeshData>();

			RenderTexture heightMap = heightProcessor.ProcessHeightMap();

			meshNormalsCompute.SetTexture(0, "HeightMap", heightMap);

			polygonProcessor.Init(heightMap, coastline.Read());

			// Create buffer containing sphere points
			Vector3[] spherePoints = IcoSphere.Generate(maxResolution).vertices;
			//Maths.Random.ShuffleArray(spherePoints, new System.Random(0));
			spherePointsBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(spherePoints.Length);
			spherePointsBuffer.SetData(spherePoints);

			gridSpherePointsBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(IcoSphere.Generate(minResolution).vertices);

			// Set other compute data
			innerVertices2DBuffer = ComputeHelper.CreateAppendBuffer<Vector2>(capacity: spherePointsBuffer.count);
			vertexCompute.SetBuffer(compute2DVerticesKernel, "SpherePoints", spherePointsBuffer);
			vertexCompute.SetInt("numSpherePoints", spherePointsBuffer.count);
			vertexCompute.SetBuffer(compute2DVerticesKernel, "InnerVertices2D", innerVertices2DBuffer);
			vertexCompute.SetTexture(compute2DVerticesKernel, "HeightMap", heightMap);
			vertexCompute.SetTexture(assignVertexHeightsKernel, "HeightMap", heightMap);

			vertexCompute.SetFloat("stepSize", stepSize);
			vertexCompute.SetFloat("errorThreshold", errorThreshold);

			totalVertexCount = 0;
			StartCoroutine(GenerateAllCountries());
		}

		IEnumerator GenerateAllCountries()
		{
			for (int countryIndex = 0; countryIndex < countries.Length; countryIndex++)
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				PolygonMeshData[] countryMeshData = GenerateCountry(countryIndex);

				// Combine country meshes
				string countryName = countries[countryIndex].name;
				SimpleMeshData[] mergedMeshes = MergeCountryPolygons(countryMeshData, countryName);

				allCombinedMeshes.AddRange(mergedMeshes);
				allPolygonMeshData.AddRange(countryMeshData);

				// Display meshes
				SpawnMeshes(mergedMeshes);

				// Update debug info
				numMeshesBeforeCombining += countryMeshData.Length;
				numMeshesAfterCombining += mergedMeshes.Length;

				yield return null; // Wait until next frame so screen can update to show progress
			}

			Debug.Log($"Generation Complete.");
			Release();
			NotifyGenerationComplete();
		}


		PolygonMeshData[] GenerateCountry(int countryIndex)
		{
			Country country = countries[countryIndex];
			List<PolygonMeshData> countryPolygonsMeshData = new List<PolygonMeshData>();

			for (int i = 0; i < country.shape.polygons.Length; i++)
			{
				Polygon polygon = country.shape.polygons[i];
				PolygonMeshData polygonMeshData = CreateCountryPolygonMesh(polygon);
				if (polygonMeshData != null)
				{
					polygonMeshData.meshData.name = country.name;
					countryPolygonsMeshData.Add(polygonMeshData);
				}
			}

			return countryPolygonsMeshData.ToArray();
		}

		PolygonMeshData CreateCountryPolygonMesh(Polygon polygon)
		{
			Path coordinatePath = polygon.paths[0];
			ComputeHelper.ResetAppendBuffer(innerVertices2DBuffer);

			// Create bounding box of polygon's sphere points
			Bounds3D sphereBounds = new Bounds3D();
			foreach (Coordinate coord in coordinatePath.points)
			{
				Vector3 spherePoint = GeoMaths.CoordinateToPoint(coord);
				sphereBounds.GrowToInclude(spherePoint);
			}

			// Upload polygon data
			vertexCompute.SetVector("polygonCentre3D", sphereBounds.Centre);
			vertexCompute.SetVector("polygonHalfSize3D", sphereBounds.HalfSize);

			ComputeBuffer polygonBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(coordinatePath.NumPoints);
			polygonBuffer.SetData(coordinatePath.GetPointsAsVector2());
			vertexCompute.SetBuffer(compute2DVerticesKernel, "Polygon2D", polygonBuffer);
			vertexCompute.SetInt("numPolygonPoints", polygonBuffer.count);



			// Run compute shader to find points inside polygon to use as vertices, and fetch results
			// Run for detail
			vertexCompute.SetFloat("errorThreshold", errorThreshold);
			vertexCompute.SetBuffer(compute2DVerticesKernel, "SpherePoints", spherePointsBuffer);
			vertexCompute.SetInt("numSpherePoints", spherePointsBuffer.count);
			ComputeHelper.Dispatch(vertexCompute, spherePointsBuffer.count, kernelIndex: compute2DVerticesKernel);

			// Run for base grid
			vertexCompute.SetFloat("errorThreshold", 0);
			vertexCompute.SetBuffer(compute2DVerticesKernel, "SpherePoints", gridSpherePointsBuffer);
			vertexCompute.SetInt("numSpherePoints", gridSpherePointsBuffer.count);
			ComputeHelper.Dispatch(vertexCompute, gridSpherePointsBuffer.count, kernelIndex: compute2DVerticesKernel);


			Coordinate[] innerPoints = ComputeHelper.ReadDataFromBuffer<Coordinate>(innerVertices2DBuffer, isAppendBuffer: true);
			ComputeHelper.Release(polygonBuffer);

			// Reproject points to be less distorted for triangulation
			var processedPolygon = polygonProcessor.ProcessPolygon(polygon, innerPoints);
			if (!processedPolygon.IsValid)
			{
				Debug.Log("Skip");
				return null;
			}

			// ---- Triangulate ----
			int[] triangles = TerrainGeneration.Triangulator.Triangulate(processedPolygon.reprojectedOutline, processedPolygon.reprojectedInnerPoints, processedPolygon.reprojectedHoles);

			// Assign heights to vertices:
			// At this stage, vertices are all points on unit sphere.
			// After this they will have magntitude (1 + h) where h is the corresponding value in the heightmap [0, 1]
			ComputeBuffer vertexBuffer = ComputeHelper.CreateStructuredBuffer(processedPolygon.spherePoints);
			vertexCompute.SetBuffer(assignVertexHeightsKernel, "Vertices", vertexBuffer);
			vertexCompute.SetInt("numVertices", vertexBuffer.count);

			// Run the compute shader to assign heights, and then fetch the results
			ComputeHelper.Dispatch(vertexCompute, vertexBuffer.count, kernelIndex: assignVertexHeightsKernel);
			Vector3[] vertices = ComputeHelper.ReadDataFromBuffer<Vector3>(vertexBuffer, isAppendBuffer: false);
			Vector3[] outlineVertices = new Vector3[processedPolygon.reprojectedOutline.Length];


			// Modify heights based on world radius and height multiplier
			for (int i = 0; i < vertices.Length; i++)
			{
				float heightT = vertices[i].magnitude - 1; // vertex magnitude is calculated in compute shader as 1 + heightT
				float height = Mathf.Max(minHeight, heightSettings.heightMultiplier * heightT);
				vertices[i] = vertices[i].normalized * (heightSettings.worldRadius + height);

				if (i < processedPolygon.reprojectedOutline.Length)
				{
					// Clamp coast points to zero
					if (processedPolygon.outlineCoastFlags[i])
					{
						vertices[i] = vertices[i].normalized * heightSettings.worldRadius;
					}
					outlineVertices[i] = vertices[i];
				}
			}

			// Normals
			meshNormalsCompute.SetBuffer(0, "Vertices", vertexBuffer);
			meshNormalsCompute.SetInt("numVerts", vertexBuffer.count);
			meshNormalsCompute.SetFloat("stepSize", normalsStepSize);
			meshNormalsCompute.SetFloat("worldRadius", heightSettings.worldRadius);
			meshNormalsCompute.SetFloat("heightMultiplier", heightSettings.heightMultiplier);
			ComputeBuffer normalsBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(vertexBuffer.count);
			meshNormalsCompute.SetBuffer(0, "Normals", normalsBuffer);
			ComputeHelper.Dispatch(meshNormalsCompute, vertexBuffer.count);
			Vector3[] normals = ComputeHelper.ReadDataFromBuffer<Vector3>(normalsBuffer, false);

			//Release
			ComputeHelper.Release(vertexBuffer, normalsBuffer);
			totalVertexCount += vertices.Length;

			SimpleMeshData meshData = new SimpleMeshData(vertices, triangles, normals);
			PolygonMeshData polygonMeshData = new PolygonMeshData()
			{
				meshData = meshData,
				bounds = new Bounds3D(vertices),
			};
			polygonMeshData.outline = outlineVertices;

			return polygonMeshData;

		}

		void SpawnMeshes(SimpleMeshData[] meshData)
		{

			for (int i = 0; i < meshData.Length; i++)
			{
				MeshHelper.CreateRendererObject(meshData[i].name, meshData[i], parent: meshHolder, material: testMat);
			}
		}


		SimpleMeshData[] MergeCountryPolygons(PolygonMeshData[] data, string countryName)
		{

			SimpleMeshData[] meshData = new SimpleMeshData[0];

			switch (meshCombineMode)
			{
				case MeshCombineMode.OneMeshPerPolygon:
					meshData = new SimpleMeshData[data.Length];
					for (int i = 0; i < data.Length; i++)
					{
						meshData[i] = data[i].meshData;
					}
					break;
				case MeshCombineMode.OneMeshPerCountry:
					meshData = new SimpleMeshData[] { CombineCountryPolygons_SingleMesh(data) };
					break;
				case MeshCombineMode.MultipleMeshesPerCountry:
					meshData = CombineCountryPolygons_MultipleMeshesAllowed(data);
					break;
			}


			// Name
			for (int i = 0; i < meshData.Length; i++)
			{
				string meshName = countryName;
				if (meshData.Length > 1)
				{
					meshName += $" ({i + 1} of {meshData.Length})";
				}
				meshData[i].name = meshName;
			}

			return meshData;


		}

		SimpleMeshData CombineCountryPolygons_SingleMesh(PolygonMeshData[] allPolygonData)
		{
			SimpleMeshData meshData = allPolygonData[0].meshData;
			for (int i = 1; i < allPolygonData.Length; i++)
			{
				meshData.Combine(allPolygonData[i].meshData);
			}

			return meshData;
		}

		SimpleMeshData[] CombineCountryPolygons_MultipleMeshesAllowed(PolygonMeshData[] allPolygonData)
		{
			List<PolygonMeshData> allCombinedData = new List<PolygonMeshData>(allPolygonData);
			// Combining works by iterating over all pairs of polygons in the country and finding the pair whose combination would result
			// in the smallest increase in the bounding box. This pair is then merged into a single mesh (provided additional constraints
			// are met, such as the new bounding box not being too big). The process repeats until no more merges are available).
			// The purpose of this is to reduce the total number of meshes, without adversely affecting frustum culling in cases
			// where a country has overseas territories for example.
			while (true)
			{
				int bestIndexA = -1;
				int bestIndexB = -1;
				float minVolumeIncrease = float.MaxValue;

				for (int i = 0; i < allCombinedData.Count - 1; i++)
				{
					for (int j = i + 1; j < allCombinedData.Count; j++)
					{
						var combineA = allCombinedData[i];
						var combineB = allCombinedData[j];
						Bounds3D combinedBounds = Bounds3D.Combine(combineA.bounds, combineB.bounds);
						float volumeIncrease = combinedBounds.Volume - Mathf.Max(combineA.bounds.Volume, combineB.bounds.Volume);

						if (volumeIncrease < minVolumeIncrease && IsValidBoundsChange(combinedBounds.Volume, volumeIncrease))
						{
							minVolumeIncrease = volumeIncrease;
							bestIndexA = i;
							bestIndexB = j;
						}
					}
				}

				if (bestIndexA != -1 && bestIndexB != -1)
				{
					var combineA = allCombinedData[bestIndexA];
					var combineB = allCombinedData[bestIndexB];
					SimpleMeshData combinedMeshData = SimpleMeshData.Combine(combineA.meshData, combineB.meshData);
					Bounds3D combinedBounds = Bounds3D.Combine(combineA.bounds, combineB.bounds);
					PolygonMeshData combinedData = new PolygonMeshData() { meshData = combinedMeshData, bounds = combinedBounds };
					allCombinedData.RemoveAt(bestIndexB);
					allCombinedData.RemoveAt(bestIndexA);
					allCombinedData.Add(combinedData);
				}
				else
				{
					// No more merges could be found
					break;
				}
			}

			SimpleMeshData[] combinedMeshes = new SimpleMeshData[allCombinedData.Count];
			for (int i = 0; i < allCombinedData.Count; i++)
			{
				combinedMeshes[i] = allCombinedData[i].meshData;
			}
			return combinedMeshes;


			bool IsValidBoundsChange(float newVolume, float volumeIncrease)
			{
				const float combineThresholdT = 0.025f;
				float sphereVolume = 4 / 3f * Mathf.PI * Mathf.Pow(heightSettings.worldRadius, 3);
				float threshold = combineThresholdT * sphereVolume;
				return newVolume < threshold || volumeIncrease < threshold * (5 / 100f);
			}
		}

		public override void Save()
		{
			// Save terrain mesh
			foreach (var meshData in allCombinedMeshes)
			{
				meshData.Optimize();
			}

			byte[] bytes = MeshSerializer.MeshesToBytes(allCombinedMeshes.ToArray());
			FileHelper.SaveBytesToFile(SavePath, meshSaveFileName, bytes, log: true);

			// Save outline paths
			List<Outline> allOutlinesTest = new List<Outline>();
			for (int i = 0; i < allPolygonMeshData.Count; i++)
			{
				allOutlinesTest.Add(new Outline() { path = allPolygonMeshData[i].outline });
			}

			string outlineJson = JsonUtility.ToJson(new AllOutlines() { paths = allOutlinesTest.ToArray() });
			FileHelper.SaveTextToFile(SavePath, outlinesSaveFileName, "json", outlineJson, log: true);

		}

		public override void Load()
		{
			var info = MeshLoader.Load(loadFile, testMat, transform, useStaticBatching: false);
			totalVertexCount = info.vertexCount;
			numMeshesAfterCombining = info.numMeshes;
			numMeshesBeforeCombining = info.numMeshes;
		}


		class PolygonMeshData
		{
			public SimpleMeshData meshData;
			public Bounds3D bounds; // bounds of vertices on unit sphere
			public Vector3[] outline;
		}

		[System.Serializable]
		public struct AllOutlines
		{
			public Outline[] paths;
		}

		[System.Serializable]
		public struct Outline
		{
			public Vector3[] path;
		}

		void OnDestroy()
		{
			Release();
		}

		void Release()
		{
			ComputeHelper.Release(spherePointsBuffer, innerVertices2DBuffer, gridSpherePointsBuffer);
		}

		void OnValidate()
		{
			numSpherePointsMax = IcoSphere.NumVerticesFromResolution(maxResolution);
			numSpherePointsMin = IcoSphere.NumVerticesFromResolution(minResolution);
		}

	}
}