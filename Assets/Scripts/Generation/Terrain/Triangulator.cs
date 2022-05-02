using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

namespace TerrainGeneration
{
	public static class Triangulator
	{


		public static int[] Triangulate(Vector2[] outlinePoints, Vector2[] innerPoints)
		{
			return Triangulate(outlinePoints, innerPoints, holes: null, false);
		}

		public static int[] Triangulate(Vector2[] outlinePoints, Vector2[] innerPoints = null, bool reverseTriangleOrder = false)
		{
			return Triangulate(outlinePoints, innerPoints, holes: null, reverseTriangleOrder);
		}

		public static int[] Triangulate(Vector2[] outlinePoints, Vector2[] innerPoints = null, Vector2[] hole = null, bool reverseTriangleOrder = false)
		{
			return Triangulate(outlinePoints, innerPoints, new Vector2[][] { hole }, reverseTriangleOrder);
		}

		public static int[] Triangulate(Polygon polygon, Coordinate[] innerPoints, bool reverseTriangleOrder = false)
		{
			Vector2[] outlinePoints = polygon.paths[0].GetPointsAsVector2();
			Vector2[][] holes = new Vector2[polygon.NumHoles][];

			for (int i = 0; i < polygon.NumHoles; i++)
			{
				holes[i] = polygon.Holes[i].GetPointsAsVector2();
			}

			int[] triangles = Triangulate(polygon.paths[0].GetPointsAsVector2(), Path.GetPointsAsVector2(innerPoints), holes, reverseTriangleOrder);
			return triangles;
		}

		public static int[] Triangulate(Vector2[] outlinePoints, Vector2[] innerPoints = null, Vector2[][] holes = null, bool reverseTriangleOrder = false)
		{
			var polygon = new TriangleNet.Geometry.Polygon();

			polygon.Add(new Contour(PointsToVertices(outlinePoints, 0)), hole: false);
			if (innerPoints != null)
			{
				polygon.Points.AddRange(PointsToVertices(innerPoints, polygon.Points.Count));
			}
			if (holes != null)
			{
				foreach (var hole in holes)
				{
					polygon.Add(new Contour(PointsToVertices(hole, polygon.Points.Count)), hole: true);
				}
			}



			//Debug.Log("Triangulating: " + outlinePoints.Length + " " + innerPoints.Length);
			var triangulation = polygon.Triangulate();
			//Debug.Log($"Triangulation of {innerPoints.Length + outlinePoints.Length} verts: {sw.ElapsedMilliseconds} ms");
			int[] triangles = new int[triangulation.Triangles.Count * 3];

			int triangleIndex = 0;
			foreach (var tri in triangulation.Triangles)
			{
				for (int i = 0; i < 3; i++)
				{
					int triVertIndex = (!reverseTriangleOrder) ? 2 - i : i;
					triangles[triangleIndex * 3 + i] = tri.GetVertex(triVertIndex).index;
				}
				triangleIndex++;
			}


			return triangles;
		}

		static Vertex[] PointsToVertices(Vector2[] points, int startI)
		{
			var verts = new Vertex[points.Length];
			int i = 0;
			foreach (var p in points)
			{
				var vertex = new Vertex(p.x, p.y);
				vertex.index = startI + i;
				verts[i] = vertex;
				i++;
			}

			return verts;
		}

	}
}
