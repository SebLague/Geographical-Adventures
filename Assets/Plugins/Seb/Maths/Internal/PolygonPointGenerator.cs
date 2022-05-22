using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.MathsHelper
{
	public class PolygonPointGenerator
	{
		public readonly Vector2[] vertices;
		public readonly int[] triangles;

		public readonly float[] cumulativeArea;
		public readonly float areaSum;
		System.Random prng;

		public PolygonPointGenerator(Vector2[] polygonPoints)
		{
			prng = new System.Random();
			Seb.MathsHelper.Triangulation.Polygon polygon = new Triangulation.Polygon(polygonPoints);
			triangles = Seb.MathsHelper.Triangulation.Triangulator.Triangulate(polygon);
			vertices = polygon.points;

			cumulativeArea = new float[triangles.Length / 3];
			areaSum = 0;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector2 a = vertices[triangles[i]];
				Vector2 b = vertices[triangles[i + 1]];
				Vector2 c = vertices[triangles[i + 2]];
				float area = Maths.SignedTriangleArea2D(a, b, c);
				areaSum += area;

				int triIndex = i / 3;
				cumulativeArea[triIndex] = areaSum;


				Debug.DrawLine(a, b, Color.red);
				Debug.DrawLine(c, b, Color.red);
				Debug.DrawLine(c, a, Color.red);

			}

		}

		public void SetSeed(int seed)
		{
			prng = new System.Random(seed);
		}

		public Vector2[] GetRandomPoints(int num)
		{
			Vector2[] points = new Vector2[Mathf.Max(0, num)];
			for (int i = 0; i < points.Length; i++)
			{
				points[i] = GetNextRandomPoint();
			}
			return points;
		}

		public Vector2 GetNextRandomPoint()
		{
			float randomT = (float)prng.NextDouble();
			float randomAreaSum = randomT * areaSum;

			int triIndex = Search(cumulativeArea, randomAreaSum);

			Vector2 a = vertices[triangles[triIndex * 3]];
			Vector2 b = vertices[triangles[triIndex * 3 + 1]];
			Vector2 c = vertices[triangles[triIndex * 3 + 2]];
			return Maths.RandomPointInTriangle(a, b, c, prng);
		}

		int Search(float[] values, float searchValue)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] > searchValue)
				{
					return Mathf.Max(0, i);
				}
			}

			return 0;
		}
	}
}