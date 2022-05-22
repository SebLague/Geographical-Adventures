using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{
	public class PolygonProcessor : MonoBehaviour
	{

		public ComputeShader polygonRefineCompute;
		public float minSubdivisionLength;
		public int numSamples;
		public float errorThreshold;
		public int minInnerPointCount;
		public float maxDstBetweenPoints;

		HashSet<Vector2> coastlineHash;

		public void Init(RenderTexture heightMap, Path[] coastline)
		{
			coastlineHash = new HashSet<Vector2>();
			foreach (Path path in coastline)
			{
				foreach (Coordinate coord in path.points)
				{
					coastlineHash.Add(coord.ToVector2());
				}
			}
			polygonRefineCompute.SetTexture(0, "HeightMap", heightMap);
		}

		public ProcessedPolygon ProcessPolygon(Polygon polygon, Coordinate[] innerPoints)
		{

			// Number of inner points is below desired threshold, so generate some extra points inside the polygon
			if (innerPoints.Length < minInnerPointCount)
			{
				Coordinate[] extraInnerPoints = GenerateExtraInnerPoints(polygon.paths[0].GetPointsAsVector2(false));
				AppendArray(ref innerPoints, extraInnerPoints);
			}


			// ---- Process the paths by marking which points lie on the coast, and also inserting points to better match the terrain ----
			// First path defines the shape of the polygon, any subsequent paths are holes to be cut out
			int numHoles = polygon.paths.Length - 1;
			ProcessedOutline[] processedOutlines = new ProcessedOutline[polygon.paths.Length];
			for (int i = 0; i < processedOutlines.Length; i++)
			{
				processedOutlines[i] = ProcessOutline(polygon.paths[i].points);
			}

			// ---- Create list of all sphere points (in order: outline points, inner points, hole points) ----
			List<Vector3> allSpherePoints = new List<Vector3>();
			Vector3[] outlineSpherePoints = ConvertToSpherePoints(processedOutlines[0].coordinates);
			allSpherePoints.AddRange(outlineSpherePoints);
			allSpherePoints.AddRange(ConvertToSpherePoints(innerPoints));
			// Add sphere points for hole paths
			for (int i = 1; i < processedOutlines.Length; i++)
			{
				allSpherePoints.AddRange(ConvertToSpherePoints(processedOutlines[i].coordinates));

			}

			// ---- Reproject points to minimize distortion (for nicer triangulation) ----
			// Calculate bounds of the polygon (this depends only on the first path, since other paths are holes)
			Bounds3D outlineBounds = new Bounds3D(outlineSpherePoints);
			Coordinate boundsCentre = GeoMaths.PointToCoordinate(outlineBounds.Centre.normalized);

			// Reproject based on bounds centre
			for (int i = 0; i < processedOutlines.Length; i++)
			{
				processedOutlines[i].coordinates = Reproject(processedOutlines[i].coordinates, boundsCentre);
			}

			Coordinate[] reprojectedInnerPoints = Reproject(innerPoints, boundsCentre);

			// Return result
			ProcessedPolygon processedPolygon = new ProcessedPolygon(processedOutlines, reprojectedInnerPoints, allSpherePoints.ToArray());
			return processedPolygon;

		}

		static void AppendArray<T>(ref T[] array, T[] arrayToAppend)
		{
			int originalLength = array.Length;
			System.Array.Resize(ref array, array.Length + arrayToAppend.Length);
			System.Array.Copy(arrayToAppend, 0, array, originalLength, arrayToAppend.Length);
		}


		// Refine the outline to better match the terrain height map by inserting points.
		ProcessedOutline ProcessOutline(Coordinate[] coords)
		{
			//Debug.Log("Before: " + coords.Length);
			coords = RemoveConsecutiveDuplicatesAndEdgePoints(coords);
			//Debug.Log("After: " + coords.Length);
			bool[] coastFlags = CreateCoastFlags(coords);

			for (int i = 0; i < 50; i++)
			{
				int numBefore = coords.Length;
				(coords, coastFlags) = InsertPointsByDistance(coords, coastFlags);
				int numAdded = coords.Length - numBefore;
				if (numAdded == 0)
				{
					break;
				}
			}

			(coords, coastFlags) = InsertPointsByHeightError(coords, coastFlags);


			return new ProcessedOutline() { coordinates = coords, coastFlags = coastFlags };

			// Create array of flags indicating whether or not point is coastal
			bool[] CreateCoastFlags(Coordinate[] coords)
			{
				bool[] coastFlags = new bool[coords.Length];
				for (int i = 0; i < coords.Length; i++)
				{
					Vector2 point = coords[i].ToVector2();
					coastFlags[i] = coastlineHash.Contains(point);
				}
				return coastFlags;
			}
		}

		public static Polygon RemoveDuplicatesAndEdgePoints(Polygon polygon)
		{
			Path[] processedPaths = new Path[polygon.paths.Length];
			for (int i = 0; i < processedPaths.Length; i++)
			{
				processedPaths[i] = new Path(RemoveConsecutiveDuplicatesAndEdgePoints(polygon.paths[i].points));
			}

			return new Polygon(processedPaths);
		}


		// Returns a new array of coordinates with duplicates and consecutive edge points removed
		public static Coordinate[] RemoveConsecutiveDuplicatesAndEdgePoints(Coordinate[] coords)
		{
			coords = RemoveConsecutiveEdgePoints(coords);
			coords = RemoveConsecutiveDuplicates(coords);
			return coords;

			Coordinate[] RemoveConsecutiveDuplicates(Coordinate[] coords)
			{
				List<Coordinate> processedCoords = new List<Coordinate>();
				processedCoords.Add(coords[0]);
				Vector3 prevSpherePoint = GeoMaths.CoordinateToPoint(coords[0]);

				for (int i = 1; i < coords.Length; i++)
				{
					int nextIndex = (i + 1) % coords.Length;
					Vector3 spherePoint = GeoMaths.CoordinateToPoint(coords[i]);
					Vector3 nextSpherePoint = GeoMaths.CoordinateToPoint(coords[nextIndex]);

					if (!IsDuplicate(spherePoint, prevSpherePoint) && !IsDuplicate(spherePoint, nextSpherePoint))
					{
						processedCoords.Add(coords[i]);
						prevSpherePoint = spherePoint;
					}
				}

				return processedCoords.ToArray();

				bool IsDuplicate(Vector3 a, Vector3 b)
				{
					const float duplicateThreshold = 0.000000001f;
					return (a - b).sqrMagnitude < duplicateThreshold;
				}
			}


			// Remove consecutive edge points (points on left/right/top/bottom edges of map)
			Coordinate[] RemoveConsecutiveEdgePoints(Coordinate[] coords)
			{
				List<Coordinate> processedCoords = new List<Coordinate>();

				for (int i = 0; i < coords.Length; i++)
				{
					int prevIndex = (i - 1 + coords.Length) % coords.Length;
					int nextIndex = (i + 1) % coords.Length;
					Coordinate prevCoord = coords[prevIndex];
					Coordinate coord = coords[i];
					Coordinate nextCoord = coords[nextIndex];

					if (!IsEdgePoint(coord) || (!IsEdgePoint(prevCoord) || !IsEdgePoint(nextCoord)))
					{
						processedCoords.Add(coords[i]);
					}
				}
				return processedCoords.ToArray();


				bool IsEdgePoint(Coordinate coord)
				{
					const float threshold = 0.0001f;
					bool isPole = Mathf.PI / 2 - Mathf.Abs(coord.latitude) < threshold;
					bool isHorizontalSeam = Mathf.PI - Mathf.Abs(coord.longitude) < threshold;
					return isPole || isHorizontalSeam;
				}
			}
		}



		(Coordinate[], bool[] coastFlags) InsertPointsByDistance(Coordinate[] coords, bool[] coastFlags)
		{
			List<InsertInfo> insertInfo = new List<InsertInfo>();
			for (int i = 0; i < coords.Length; i++)
			{
				Vector3 spherePoint = GeoMaths.CoordinateToPoint(coords[i]);
				Vector3 nextSpherePoint = GeoMaths.CoordinateToPoint(coords[(i + 1) % coords.Length]);
				float length = (spherePoint - nextSpherePoint).magnitude;

				if (length > maxDstBetweenPoints)
				{

					Vector3 interpolatedSpherePoint = Vector3.Lerp(spherePoint, nextSpherePoint, 0.5f).normalized; // linear approximation since sphere points assumed to already be close together
					Coordinate coord = GeoMaths.PointToCoordinate(interpolatedSpherePoint);
					insertInfo.Add(new InsertInfo() { coordToInsert = coord, insertAfterPointIndex = i });
				}
			}

			return InsertPoints(coords, coastFlags, insertInfo.ToArray());
		}

		(Coordinate[], bool[] coastFlags) InsertPointsByHeightError(Coordinate[] coords, bool[] coastFlags)
		{
			InsertInfo[] insertInfo = CalculateOutlinePointsToInsert(coords);
			return InsertPoints(coords, coastFlags, insertInfo);
		}

		(Coordinate[], bool[] coastFlags) InsertPoints(Coordinate[] coords, bool[] coastFlags, InsertInfo[] insertInfo)
		{
			// ---- Handle insertion of new points ----
			var refinedPathList = new LinkedList<(Coordinate coord, bool coastalFlag)>();
			var originalNodes = new LinkedListNode<(Coordinate, bool)>[coords.Length];

			for (int i = 0; i < coords.Length; i++)
			{
				originalNodes[i] = refinedPathList.AddLast((coords[i], coastFlags[i]));
			}

			// Insert
			for (int i = 0; i < insertInfo.Length; i++)
			{
				InsertInfo insert = insertInfo[i];
				var nodeToInsertPointAfter = originalNodes[insert.insertAfterPointIndex];


				bool isCoastPoint = coastFlags[insert.insertAfterPointIndex] && coastFlags[(insert.insertAfterPointIndex + 1) % coastFlags.Length];
				refinedPathList.AddAfter(nodeToInsertPointAfter, (insert.coordToInsert, isCoastPoint));
			}

			// Convert linked list to ProcessedOutline
			Coordinate[] refinedCoords = new Coordinate[refinedPathList.Count];
			bool[] refinedCoastFlags = new bool[refinedPathList.Count];
			var refinedNode = refinedPathList.First;

			for (int i = 0; i < refinedPathList.Count; i++)
			{
				refinedCoords[i] = refinedNode.Value.coord;
				refinedCoastFlags[i] = refinedNode.Value.coastalFlag;
				refinedNode = refinedNode.Next;
			}

			return (refinedCoords, refinedCoastFlags);
		}

		public class ProcessedOutline
		{
			public Coordinate[] coordinates;
			public bool[] coastFlags;

			public int NumPoints
			{
				get
				{
					return coordinates.Length;
				}
			}
		}

		Vector3[] ConvertToSpherePoints(Coordinate[] coords)
		{
			Vector3[] spherePoints = new Vector3[coords.Length];
			for (int i = 0; i < coords.Length; i++)
			{
				spherePoints[i] = GeoMaths.CoordinateToPoint(coords[i]);
			}
			return spherePoints;
		}

		// Run compute shader to calculate where points should be inserted along the outline to better match height map
		InsertInfo[] CalculateOutlinePointsToInsert(Coordinate[] coords)
		{
			ComputeBuffer polygonBuffer = ComputeHelper.CreateStructuredBuffer(Path.GetPointsAsVector2(coords));
			ComputeBuffer resultBuffer = ComputeHelper.CreateAppendBuffer<InsertInfo>(capacity: coords.Length);
			polygonRefineCompute.SetInt("numPolygonPoints", polygonBuffer.count);
			polygonRefineCompute.SetBuffer(0, "Polygon", polygonBuffer);
			polygonRefineCompute.SetBuffer(0, "Result", resultBuffer);
			polygonRefineCompute.SetFloat("minSubdivisionLength", minSubdivisionLength);
			polygonRefineCompute.SetFloat("errorThreshold", errorThreshold);
			polygonRefineCompute.SetInt("numSamples", numSamples);

			// Dispatch
			ComputeHelper.Dispatch(polygonRefineCompute, polygonBuffer.count - 1);
			// Read result back from gpu
			InsertInfo[] result = ComputeHelper.ReadDataFromBuffer<InsertInfo>(resultBuffer, isAppendBuffer: true);
			// Release buffers
			ComputeHelper.Release(polygonBuffer, resultBuffer);
			return result;
		}

		public static Coordinate Reproject(Coordinate coord, Coordinate centre)
		{
			Vector3 spherePos = GeoMaths.CoordinateToPoint(coord);
			// Rotate sphere so that country is centered at equator
			Vector3 rotatedSpherePos = Seb.Maths.RotateAroundAxis(spherePos, Vector3.up, centre.longitude);
			rotatedSpherePos = Seb.Maths.RotateAroundAxis(rotatedSpherePos, Vector3.right, -centre.latitude);

			return GeoMaths.PointToCoordinate(rotatedSpherePos);
		}


		public static Coordinate[] Reproject(Coordinate[] coords, Coordinate centre)
		{

			Coordinate[] reprojectedCoords = new Coordinate[coords.Length];
			for (int i = 0; i < coords.Length; i++)
			{
				reprojectedCoords[i] = Reproject(coords[i], centre);
			}
			return reprojectedCoords;
		}

		public static (Polygon reprojectedPolygon, Coordinate[] reprojectedInnerPoints) Reproject(Polygon polygon, Coordinate[] innerPoints)
		{
			Bounds3D bounds = new Bounds3D();
			for (int i = 0; i < polygon.paths[0].NumPoints; i++)
			{
				Vector3 spherePoint = GeoMaths.CoordinateToPoint(polygon.paths[0].points[i]);
				bounds.GrowToInclude(spherePoint);
			}

			Coordinate polygonCentre = GeoMaths.PointToCoordinate(bounds.Centre.normalized);

			Path[] reprojectedPaths = new Path[polygon.paths.Length];
			for (int i = 0; i < reprojectedPaths.Length; i++)
			{
				reprojectedPaths[i] = new Path(Reproject(polygon.paths[i].points, polygonCentre));
			}

			Coordinate[] reprojectedInnerPoints = Reproject(innerPoints, polygonCentre);

			return (new Polygon(reprojectedPaths), reprojectedInnerPoints);
		}

		Coordinate[] GenerateExtraInnerPoints(Vector2[] polygon)
		{

			int[] triangles = TerrainGeneration.Triangulator.Triangulate(polygon, null, false);
			int numTriangles = triangles.Length / 3;
			Coordinate[] points = new Coordinate[numTriangles];

			for (int i = 0; i < numTriangles; i++)
			{
				Vector2 a = polygon[triangles[i * 3]];
				Vector2 b = polygon[triangles[i * 3 + 1]];
				Vector2 c = polygon[triangles[i * 3 + 2]];
				points[i] = Coordinate.FromVector2((a + b + c) / 3);
			}

			return points;
		}



		struct ReprojectionResult
		{
			public Vector2[] coords;
			public Vector3[] spherePoints;
			public bool[] coastalFlags;
		}


		public class ProcessedPolygon
		{
			public Vector2[] reprojectedOutline;
			public bool[] outlineCoastFlags;

			public Vector2[] reprojectedInnerPoints;
			public Vector2[][] reprojectedHoles;

			public Vector3[] spherePoints;

			public ProcessedPolygon(ProcessedOutline[] processedPaths, Coordinate[] processedInnerPoints, Vector3[] spherePoints)
			{
				this.reprojectedOutline = Path.GetPointsAsVector2(processedPaths[0].coordinates);
				this.outlineCoastFlags = processedPaths[0].coastFlags;
				this.reprojectedInnerPoints = Path.GetPointsAsVector2(processedInnerPoints);
				this.spherePoints = spherePoints;

				int numHoles = processedPaths.Length - 1;
				this.reprojectedHoles = new Vector2[numHoles][];
				for (int i = 0; i < numHoles; i++)
				{
					this.reprojectedHoles[i] = Path.GetPointsAsVector2(processedPaths[i + 1].coordinates);
				}
			}


			public bool IsValid
			{
				get
				{
					return reprojectedOutline.Length >= 3;
				}
			}
		}

		public struct InsertInfo
		{
			public int insertAfterPointIndex;
			public Coordinate coordToInsert;
		}
	}

}