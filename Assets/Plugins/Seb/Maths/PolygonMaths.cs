using UnityEngine;

// Some maths relating to polygons

namespace Seb
{
	public static partial class Maths
	{
		/// <summary>
		/// Test if a 2D polygon contains the given point.
		/// Points can be ordered clockwise or counterclockwise.
		/// Note: the last point in the polygon is not expected to be a duplicate of the first point.
		/// </summary>
		public static bool PolygonContainsPoint(Vector2 p, Vector2[] points)
		{
			// Algorithm by Dan Sunday
			int windingNumber = 0;
			for (int i = 0; i < points.Length; i++)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Length];

				if (a.y <= p.y)
				{
					if (b.y > p.y && PointIsOnLeftSideOfLine(p, a, b))
					{
						windingNumber++;
					}
				}
				else if (b.y <= p.y && !PointIsOnLeftSideOfLine(p, a, b))
				{
					windingNumber--;
				}
			}

			return windingNumber != 0;

			// Calculate which side of line AB point P is on
			bool PointIsOnLeftSideOfLine(Vector2 p, Vector2 a, Vector2 b)
			{
				return (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y) > 0;
			}
		}

		public static bool PolygonIsClockwise(Vector2[] points)
		{
			return PolygonSignedArea(points) > 0;
		}

		public static float PolygonArea(Vector2[] points)
		{
			return Mathf.Abs(PolygonSignedArea(points));
		}

		public static float PolygonSignedArea(Vector2[] points)
		{
			float signedArea = 0;
			for (int i = 0; i < points.Length; i++)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Length];
				signedArea += (b.x - a.x) * (b.y + a.y);
			}

			return signedArea;
		}


		/// <summary>
		/// Note: my polygon code is a bit wonky and should be rewritten at some point -- use with caution
		/// </summary>
		public static Vector2[] RandomPointsInsidePolygon(Vector2[] polygon, int numPoints)
		{
			return new Seb.MathsHelper.PolygonPointGenerator(polygon).GetRandomPoints(numPoints);
		}

		/// <summary>
		/// Note: my polygon code is a bit wonky and should be rewritten at some point -- use with caution
		/// </summary>
		public static Vector2[] RandomPointsInsidePolygon(Vector2[] polygon, int numPoints, int seed)
		{
			var gen = new Seb.MathsHelper.PolygonPointGenerator(polygon);
			gen.SetSeed(seed);
			return gen.GetRandomPoints(numPoints);
		}

		/// <summary>
		/// Note: my polygon code is a bit wonky and should be rewritten at some point -- use with caution
		/// </summary>
		public static int[] TriangulatePolygon(Vector2[] polygon)
		{
			return Seb.MathsHelper.Triangulation.Triangulator.Triangulate(polygon);
		}
	}
}