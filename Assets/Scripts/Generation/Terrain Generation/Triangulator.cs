using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

namespace TerrainGeneration
{
	public static class Triangulator
	{

		public static int testVertexCount;

		public static int[] Triangulate(Vector2[] innerPoints, Vector2[] outlinePoints, bool reverseTriangleOrder = false)
		{
			var polygon = new TriangleNet.Geometry.Polygon();

			polygon.Add(new Contour(PointsToVertices(outlinePoints, 0)));
			polygon.Points.AddRange(PointsToVertices(innerPoints, polygon.Points.Count));

			var triangulation = polygon.Triangulate();
			int[] triangles = new int[triangulation.Triangles.Count * 3];

			int triangleIndex = 0;
			foreach (var tri in triangulation.Triangles)
			{
				for (int i = 0; i < 3; i++)
				{
					int triVertIndex = (reverseTriangleOrder) ? 2 - i : i;
					triangles[triangleIndex * 3 + i] = tri.GetVertex(triVertIndex).index;
				}
				triangleIndex++;
			}

			testVertexCount = triangulation.Vertices.Count;

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
