using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainGeneration
{
	public class TerrainGenerator : MonoBehaviour
	{
		public enum Space2D { xy, xz, yz }

		[Header("Generator Settings")]
		public TerrainSettings settings;
	

		[Header("References")]
		public TerrainHeightSettings heightSettings;
		public ComputeShader vertexCompute;
		public TerrainHeightProcessor heightProcessor;
		public Material material;
		public ComputeShader meshNormalsCompute;

		[Header("Save/Load Settings")]
		public bool readFromFile;
		public string saveFileName = "terrainData";
		public TextAsset terrainLoadFile;
		public TextAsset terrainLoadFileB;

		public FaceData[] allFaceData { get; private set; }
		public bool isGenerating { get; private set; }

		Mesh[] loadedMeshes;

		public void LoadA()
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				Destroy(transform.GetChild(i).gameObject);
			}
			allFaceData = TerrainReader.LoadFromFile(terrainLoadFile);
			CreateMeshesFromData(allFaceData);
		}

		public void LoadB()
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				Destroy(transform.GetChild(i).gameObject);
			}
			allFaceData = TerrainReader.LoadFromFile(terrainLoadFileB);
			CreateMeshesFromData(allFaceData);
		}

		void Start()
		{
			heightProcessor.ProcessHeightMap();
			material.mainTexture = heightProcessor.processedHeightMap;

			if (readFromFile)
			{
				allFaceData = TerrainReader.LoadFromFile(terrainLoadFile);
				CreateMeshesFromData(allFaceData);
			}
			else
			{
				StartCoroutine(Generate());
			}

		}

		IEnumerator Generate()
		{
			isGenerating = true;


			RenderTexture dstMap = FindObjectOfType<JumpFloodTest>().result;

			Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.back, Vector3.forward };
			Space2D[] space = { Space2D.xz, Space2D.yz, Space2D.xy };
			vertexCompute.SetInt("resolution", settings.resolution);
			vertexCompute.SetTexture(0, "HeightMap", heightProcessor.processedHeightMap);
			vertexCompute.SetFloat("errorThreshold", settings.errorThreshold);
			vertexCompute.SetInt("gridCellSize", settings.gridCellSize);
			vertexCompute.SetTexture(0, "DistanceMap", dstMap);

			float faceCoveragePerSubFace = 1f / settings.numSubdivisions;
			int numFaces = 6 * settings.numSubdivisions * settings.numSubdivisions;
			allFaceData = new FaceData[numFaces];
			loadedMeshes = new Mesh[numFaces];
			int faceIndex = 0;
			int totalVertexCount = 0;

			for (int i = 0; i < 6; i++)
			{
				for (int y = 0; y < settings.numSubdivisions; y++)
				{
					for (int x = 0; x < settings.numSubdivisions; x++)
					{
						Vector2 startT = new Vector2(x, y) * faceCoveragePerSubFace;
						Vector2 endT = startT + Vector2.one * faceCoveragePerSubFace;
						FaceData faceData = CreateFace(faceNormals[i], space[i / 2], i % 2 == 0, startT, endT);
						Mesh mesh = CreateMeshDisplay(faceData, faceIndex);

						allFaceData[faceIndex] = faceData;
						loadedMeshes[faceIndex] = mesh;
						totalVertexCount += faceData.pointData.Length;
						faceIndex++;
						yield return null;
					}
				}
			}

			isGenerating = false;
			Debug.Log("Generation complete. Vertex count: " + totalVertexCount + "  Edge p: " + edgePCount + "  Reg p: " + regularPCount);
		}

		void CreateMeshesFromData(FaceData[] allFaceData)
		{
			loadedMeshes = new Mesh[allFaceData.Length];
			int totalVertCount = 0;
			for (int i = 0; i < allFaceData.Length; i++)
			{
				Mesh mesh = CreateMeshDisplay(allFaceData[i], i);
				loadedMeshes[i] = mesh;
				totalVertCount += allFaceData[i].pointData.Length;
			}
			Debug.Log("Vertex count: " + totalVertCount);
		}

		public void RegenerateNormals()
		{
			for (int i = 0; i < loadedMeshes.Length; i++)
			{
				Mesh mesh = loadedMeshes[i];
				mesh.RecalculateNormals();
			}
		}

		public void CalculateAllNormals()
		{
			for (int i = 0; i < loadedMeshes.Length; i++)
			{
				Mesh mesh = loadedMeshes[i];
				mesh.normals = CalculateNormals(mesh.vertices);
			}
		}

		public Vector3[] CalculateNormals(Vector3[] vertices)
		{
			meshNormalsCompute.SetTexture(0, "HeightMap", heightProcessor.processedHeightMap);
			meshNormalsCompute.SetFloat("worldRadius", heightSettings.worldRadius);
			meshNormalsCompute.SetFloat("heightMultiplier", heightSettings.heightMultiplier);
			meshNormalsCompute.SetFloat("stepSize", settings.normalsStepSize);


			ComputeBuffer vertexBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(vertices);
			ComputeBuffer normalsBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(vertexBuffer.count);
			meshNormalsCompute.SetBuffer(0, "Vertices", vertexBuffer);
			meshNormalsCompute.SetBuffer(0, "Normals", normalsBuffer);
			meshNormalsCompute.SetInt("numVerts", vertexBuffer.count);
			ComputeHelper.Dispatch(meshNormalsCompute, vertexBuffer.count);

			Vector3[] normals = new Vector3[vertexBuffer.count];
			normalsBuffer.GetData(normals);

			vertexBuffer.Release();
			normalsBuffer.Release();

			return normals;
		}

		int edgePCount;
		int regularPCount;

		FaceData CreateFace(Vector3 normal, Space2D space, bool reverseTriangleOrder, Vector2 startT, Vector2 endT)
		{
			// --- Compute points ---
			Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
			Vector3 axisB = Vector3.Cross(normal, axisA);

			vertexCompute.SetVector("normal", normal);
			vertexCompute.SetVector("axisA", axisA);
			vertexCompute.SetVector("axisB", axisB);
			vertexCompute.SetVector("startT", startT);
			vertexCompute.SetVector("endT", endT);
			Vector2 centre = (startT + endT) / 2f;

			const int test = 5;
			ComputeBuffer pointBuffer = ComputeHelper.CreateAppendBuffer<Vector4>(settings.resolution * settings.resolution * test);
			ComputeBuffer edgePointsBuffer = ComputeHelper.CreateAppendBuffer<Vector4>(settings.resolution * 4 * test);
			vertexCompute.SetBuffer(0, "Points", pointBuffer);
			vertexCompute.SetBuffer(0, "EdgePoints", edgePointsBuffer);

			ComputeHelper.Dispatch(vertexCompute, settings.resolution, settings.resolution);

			Vector4[] points = GetPointsFromBuffer(pointBuffer);
			Vector4[] edgePoints = GetPointsFromBuffer(edgePointsBuffer);

			ComputeHelper.Release(pointBuffer);
			ComputeHelper.Release(edgePointsBuffer);

			// --- Convert points to 2d for triangulation ---
			Vector2[] points2D = ConvertAllToVector2(points, space);
			Vector2[] edgePoints2D = ConvertAllToVector2(edgePoints, space);
			// Sort edge points clockwise
			Vector2 t = (startT + endT) / 2;
			Vector3 faceCentre = (t.x - 0.5f) * 2 * axisA + (t.y - 0.5f) * 2 * axisB;
			Vector2 faceCentre2D = ToVector2(faceCentre, space);
			SortClockwise(edgePoints, edgePoints2D, faceCentre2D);

			// Triangulate
			int[] triangles = Triangulator.Triangulate(points2D, edgePoints2D, reverseTriangleOrder);

			// Create vertex/height/normals arrays
			List<Vector4> allPoints = new List<Vector4>(edgePoints);
			allPoints.AddRange(points);
			edgePCount += edgePoints.Length;
			regularPCount += points.Length;

			Vector3[] spherePoints = new Vector3[allPoints.Count];
			Vector3[] vertices = new Vector3[allPoints.Count];
			float[] heights = new float[allPoints.Count];
			for (int i = 0; i < spherePoints.Length; i++)
			{
				float height = allPoints[i].w;
				spherePoints[i] = CubeSphere.CubePointToSpherePoint((Vector3)allPoints[i]);
				vertices[i] = spherePoints[i] * (heightSettings.worldRadius + height * heightSettings.heightMultiplier);
				heights[i] = height;
			}

			Vector3[] normals = CalculateNormals(vertices);


			FaceData data = new FaceData(spherePoints, heights, triangles, normals);
			return data;
		}


		Mesh CreateMeshDisplay(FaceData data, int faceIndex)
		{
			Mesh mesh = data.CreateMesh(heightSettings.worldRadius, heightSettings.heightMultiplier);


			GameObject meshHolder = new GameObject("Mesh Holder " + faceIndex);
			meshHolder.transform.parent = transform;
			MeshRenderer renderer = meshHolder.AddComponent<MeshRenderer>();
			MeshFilter filter = meshHolder.AddComponent<MeshFilter>();

			renderer.material = material;
			filter.mesh = mesh;
			return mesh;
		}

		Vector2[] ConvertAllToVector2(Vector4[] points, Space2D space)
		{
			Vector2[] points2D = new Vector2[points.Length];
			for (int i = 0; i < points.Length; i++)
			{
				points2D[i] = ToVector2(points[i], space);
			}
			return points2D;
		}

		Vector2 ToVector2(Vector3 p, Space2D space)
		{
			float x = p.x;
			float y = p.y;
			if (space == Space2D.xz)
			{
				y = p.z;
			}
			else if (space == Space2D.yz)
			{
				x = p.z;
			}
			return new Vector2(x, y);
		}

		void SortClockwise(Vector4[] points, Vector2[] points2D, Vector2 centre)
		{
			Vector4[] unsortedPoints = new Vector4[points.Length];
			Vector2[] unsortedPoints2D = new Vector2[points.Length];
			List<int> clockwiseIndices = new List<int>(points.Length);

			for (int i = 0; i < points.Length; i++)
			{
				clockwiseIndices.Add(i);
				unsortedPoints[i] = points[i];
				unsortedPoints2D[i] = points2D[i];
			}

			clockwiseIndices.Sort((a, b) => Compare(a, b, centre));
			for (int i = 0; i < points.Length; i++)
			{
				int sortedIndex = clockwiseIndices[i];
				points[i] = unsortedPoints[sortedIndex];
				points2D[i] = unsortedPoints2D[sortedIndex];
			}

			int Compare(int indexA, int indexB, Vector2 centre)
			{
				Vector2 a = points2D[indexA];
				Vector2 b = points2D[indexB];
				float det = (a.x - centre.x) * (b.y - centre.y) - (b.x - centre.x) * (a.y - centre.y);
				return (det > 0) ? 1 : -1;
			}
		}

		Vector4[] GetPointsFromBuffer(ComputeBuffer appendBuffer)
		{
			ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
			ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);
			int[] count = new int[1];
			countBuffer.GetData(count);
			countBuffer.Release();

			Vector4[] points = new Vector4[count[0]];
			appendBuffer.GetData(points);
			return points;
		}





	}

}