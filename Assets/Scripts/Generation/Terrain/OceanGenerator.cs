using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Seb.Meshing;
using Seb;

namespace TerrainGeneration
{
	public class OceanGenerator : Generator
	{


		[Header("Generator Settings")]
		[Min(0)] public int numCubeSubdivisions;
		[Min(0)] public int resolution;
		public bool useCoastPoints;


		[Header("References")]
		public TerrainGeneration.TerrainHeightSettings heightSettings;
		public ComputeShader oceanTileCompute;
		public Material material;
		public Texture2D landMask;
		public Coastline coastlineReader;

		[Header("Save/Load Settings")]
		public string saveFileName = "terrainData";
		public TextAsset loadFile;

		[Header("Info")]
		[Disabled] public int totalVertexCount;
		[Disabled] public int numMeshes;

		SimpleMeshData[] allGeneratedFaces;
		Coordinate[] allCoastPoints;


		protected override void Start()
		{
			base.Start();
		}

		public override void StartGenerating()
		{
			StartCoroutine(Generate());
		}

		IEnumerator Generate()
		{
			NotifyGenerationStarted();

			Path[] paths = coastlineReader.Read();
			List<Coordinate> allCoastPointsList = new List<Coordinate>();
			foreach (var path in paths)
			{
				allCoastPointsList.AddRange(path.points);
			}
			allCoastPoints = allCoastPointsList.ToArray();



			Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.back, Vector3.forward };
			Space2D[] space = { Space2D.xz, Space2D.yz, Space2D.xy };

			oceanTileCompute.SetInt("resolution", numPointsPerAxis);
			oceanTileCompute.SetTexture(0, "LandMask", landMask);

			float faceCoveragePerSubFace = 1f / numCubeSubdivisions;
			int numFaces = 6 * numCubeSubdivisions * numCubeSubdivisions;
			allGeneratedFaces = new SimpleMeshData[numFaces];
			int faceIndex = 0;

			for (int i = 0; i < 6; i++)
			{
				for (int y = 0; y < numCubeSubdivisions; y++)
				{
					for (int x = 0; x < numCubeSubdivisions; x++)
					{
						Vector2 startT = new Vector2(x, y) * faceCoveragePerSubFace;
						Vector2 endT = startT + Vector2.one * faceCoveragePerSubFace;
						SimpleMeshData faceMeshData = CreateFace(faceNormals[i], space[i / 2], i % 2 != 0, startT, endT, faceIndex);
						MeshHelper.CreateRendererObject("Ocean tile " + faceIndex, faceMeshData, material, parent: transform);
						allGeneratedFaces[faceIndex] = faceMeshData;

						totalVertexCount += faceMeshData.vertices.Length;
						numMeshes++;
						faceIndex++;
						yield return null;
					}
				}
			}

			Debug.Log("Ocean Generation Complete");
			NotifyGenerationComplete();
		}

		int numPointsPerAxis
		{
			get
			{
				return Mathf.Max(0, resolution) + 2;
			}
		}

		SimpleMeshData CreateFace(Vector3 normal, Space2D space, bool reverseTriangleOrder, Vector2 startT, Vector2 endT, int faceIndex)
		{

			Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
			Vector3 axisB = Vector3.Cross(normal, axisA);

			oceanTileCompute.SetVector("normal", normal);
			oceanTileCompute.SetVector("axisA", axisA);
			oceanTileCompute.SetVector("axisB", axisB);
			oceanTileCompute.SetVector("startT", startT);
			oceanTileCompute.SetVector("endT", endT);
			Vector2 centre = (startT + endT) / 2f;

			ComputeBuffer pointBuffer = ComputeHelper.CreateAppendBuffer<Vector3>(numPointsPerAxis * numPointsPerAxis);
			ComputeBuffer edgePointsBuffer = ComputeHelper.CreateAppendBuffer<Vector3>(numPointsPerAxis * 4); // (4 edges)
			oceanTileCompute.SetBuffer(0, "Points", pointBuffer);
			oceanTileCompute.SetBuffer(0, "EdgePoints", edgePointsBuffer);

			ComputeHelper.Dispatch(oceanTileCompute, numPointsPerAxis, numPointsPerAxis);

			// Fetch points from GPU for triangulation
			Vector3[] points = ComputeHelper.ReadDataFromBuffer<Vector3>(pointBuffer, isAppendBuffer: true);
			Vector3[] edgePoints = ComputeHelper.ReadDataFromBuffer<Vector3>(edgePointsBuffer, isAppendBuffer: true);

			ComputeHelper.Release(pointBuffer, edgePointsBuffer);




			// --- Convert points to 2d for triangulation ---
			Vector2[] points2D = VectorHelper.To2DArray(points, space);
			Vector2[] edgePoints2D = VectorHelper.To2DArray(edgePoints, space);

			// --- Handle coastlines ---
			if (useCoastPoints)
			{
				Coordinate[] coastCoordsOnFace = GetCoastPointsOnFace(allCoastPoints, normal, axisA, axisB, startT, endT);
				Vector2[] coastPoints2D = new Vector2[coastCoordsOnFace.Length];
				Vector3[] coastPoints3D = new Vector3[coastCoordsOnFace.Length];
				for (int i = 0; i < coastCoordsOnFace.Length; i++)
				{
					Vector3 spherePoint = GeoMaths.CoordinateToPoint(coastCoordsOnFace[i]);
					Vector3 cubePoint = CubeSphere.SpherePointToCubePoint(spherePoint);
					coastPoints3D[i] = cubePoint;
					coastPoints2D[i] = VectorHelper.ToVector2(cubePoint, space);
				}

				// Add
				Seb.ArrayHelper.AppendArray(ref points, coastPoints3D);
				Seb.ArrayHelper.AppendArray(ref points2D, coastPoints2D);
			}


			// Sort edge points clockwise
			SortClockwise(edgePoints, edgePoints2D, space);

			// Triangulate
			int[] triangles = TerrainGeneration.Triangulator.Triangulate(edgePoints2D, points2D, reverseTriangleOrder);


			// Create vertex/height/normals arrays
			List<Vector3> allPoints = new List<Vector3>();
			allPoints.AddRange(edgePoints);
			allPoints.AddRange(points);

			Vector3[] vertices = new Vector3[allPoints.Count];
			Vector3[] normals = new Vector3[vertices.Length];

			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 spherePoint = CubeSphere.CubePointToSpherePoint((Vector3)allPoints[i]);
				normals[i] = spherePoint;
				vertices[i] = spherePoint * heightSettings.worldRadius;
			}

			SimpleMeshData meshData = new SimpleMeshData(vertices, triangles, normals, name: $"Ocean chunk {faceIndex}");

			return meshData;
		}

		void SortClockwise(Vector3[] points3D, Vector2[] points2D, Space2D space)
		{

			Bounds2D bounds = new Bounds2D(points2D);
			Vector2 centre = bounds.Centre;

			// Sort
			for (int i = 0; i < points2D.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int relativeScore = -Compare(points2D[swapIndex], points2D[j], centre);
					if (relativeScore < 0)
					{
						(points2D[j], points2D[swapIndex]) = (points2D[swapIndex], points2D[j]);
						(points3D[j], points3D[swapIndex]) = (points3D[swapIndex], points3D[j]);
					}
				}
			}


			// Thanks to https://stackoverflow.com/a/6989383
			int Compare(Vector2 a, Vector2 b, Vector2 centre)
			{
				if (a.x - centre.x >= 0 && b.x - centre.x < 0)
					return 1;
				if (a.x - centre.x < 0 && b.x - centre.x >= 0)
					return -1;
				if (a.x - centre.x == 0 && b.x - centre.x == 0)
				{
					if (a.y - centre.y >= 0 || b.y - centre.y >= 0)
					{
						return (a.y > b.y) ? 1 : -1;
					}
					return (b.y > a.y) ? 1 : -1;
				}

				// Compute the cross product of vectors (centre -> a) x (centre -> b)
				float det = (a.x - centre.x) * (b.y - centre.y) - (b.x - centre.x) * (a.y - centre.y);
				if (det < 0)
				{
					return 1;
				}
				if (det > 0)
				{
					return -1;
				}

				// Points a and b are on the same line from the centre, so check which is closer to the centre
				float sqrDstA = (a.x - centre.x) * (a.x - centre.x) + (a.y - centre.y) * (a.y - centre.y);
				float sqrDstB = (b.x - centre.x) * (b.x - centre.x) + (b.y - centre.y) * (b.y - centre.y);
				return (sqrDstA > sqrDstB) ? 1 : -1;
			}
		}



		Coordinate[] GetCoastPointsOnFace(Coordinate[] allCoastPoints, Vector3 normal, Vector3 axisA, Vector3 axisB, Vector2 startT, Vector2 endT)
		{
			Vector3 cubeA = normal + (startT.x - 0.5f) * 2 * axisA + (startT.y - 0.5f) * 2 * axisB;
			Vector3 cubeB = normal + (endT.x - 0.5f) * 2 * axisA + (startT.y - 0.5f) * 2 * axisB;
			Vector3 cubeC = normal + (endT.x - 0.5f) * 2 * axisA + (endT.y - 0.5f) * 2 * axisB;
			Vector3 cubeD = normal + (startT.x - 0.5f) * 2 * axisA + (endT.y - 0.5f) * 2 * axisB;


			Vector2 extremeA = DropDimension(cubeA, normal);
			Vector2 extremeB = DropDimension(cubeB, normal);
			Vector2 extremeC = DropDimension(cubeC, normal);
			Vector2 extremeD = DropDimension(cubeD, normal);

			Bounds2D bounds = new Bounds2D(new Vector2[] { extremeA, extremeB, extremeC, extremeD });
			Vector2[] polygon = new Vector2[] { extremeA, extremeB, extremeC, extremeD };

			List<Coordinate> coastPointsInBounds = new List<Coordinate>();
			for (int i = 0; i < allCoastPoints.Length; i++)
			{
				Coordinate coastCoord = allCoastPoints[i];
				Vector3 coastSpherePoint = GeoMaths.CoordinateToPoint(coastCoord);
				Vector3 coastCubePoint = CubeSphere.SpherePointToCubePoint(coastSpherePoint);
				if (Vector3.Dot(coastCubePoint, normal) != 1)
				{
					continue;
				}
				Vector2 testPoint = DropDimension(coastCubePoint, normal);

				//if (Maths.Polygon.ContainsPoint(polygon, testPoint))
				if (bounds.Contains(testPoint))
				{
					coastPointsInBounds.Add(coastCoord);
				}
			}

			return coastPointsInBounds.ToArray();

			Vector2 DropDimension(Vector3 vec3D, Vector3 normal)
			{
				float xWeight = Mathf.Abs(normal.x);
				float yWeight = Mathf.Abs(normal.y);
				float zWeight = Mathf.Abs(normal.z);

				if (xWeight > yWeight && xWeight > zWeight)
				{
					return new Vector2(vec3D.y, vec3D.z);
				}
				else if (yWeight > xWeight && yWeight > zWeight)
				{
					return new Vector2(vec3D.x, vec3D.z);
				}
				else
				{
					return new Vector2(vec3D.x, vec3D.y);
				}
			}
		}


		public override void Save()
		{
			foreach (var face in allGeneratedFaces)
			{
				face.Optimize();
			}

			byte[] bytes = MeshSerializer.MeshesToBytes(allGeneratedFaces);
			FileHelper.SaveBytesToFile(FileHelper.MakePath("Assets", "Data", "Terrain"), saveFileName, bytes, log: true);

		}

		public override void Load()
		{
			if (loadFile == null)
			{
				Debug.LogError("No load file specified");
			}
			else
			{
				var info = MeshLoader.Load(loadFile, material, transform, false);
				totalVertexCount = info.vertexCount;
				numMeshes = info.numMeshes;
			}
		}

	}
}