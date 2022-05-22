using UnityEngine;

// Some maths relating to triangles

namespace Seb
{
	public static partial class Maths
	{

		/// <summary>
		/// Returns the area of a triangle in 3D space.
		/// </summary>
		public static float TriangleArea(Vector3 triA, Vector3 triB, Vector3 triC)
		{
			// Thanks to https://math.stackexchange.com/a/1951650
			Vector3 ortho = Vector3.Cross(triC - triA, triB - triA);
			float parallogramArea = ortho.magnitude;
			return parallogramArea * 0.5f;
		}

		/// <summary>
		/// Returns the signed area of a triangle in 2D space.
		/// The sign depends on the whether the points are given in clockwise (negative) or counter-clockwise (positive) order.
		/// </summary>
		public static float SignedTriangleArea2D(Vector2 triA, Vector2 triB, Vector2 triC)
		{
			return 0.5f * (-triB.y * triC.x + triA.y * (-triB.x + triC.x) + triA.x * (triB.y - triC.y) + triB.x * triC.y);
		}

		/// <summary>
		/// Determines whether the given point lies inside the 2D triangle
		/// </summary>
		public static bool TriangleContainsPoint(Vector2 triA, Vector2 triB, Vector2 triC, Vector2 p)
		{
			// Thanks to https://stackoverflow.com/a/14382692
			float area = SignedTriangleArea2D(triA, triB, triC);
			float s = (triA.y * triC.x - triA.x * triC.y + (triC.y - triA.y) * p.x + (triA.x - triC.x) * p.y) * Mathf.Sign(area);
			float t = (triA.x * triB.y - triA.y * triB.x + (triA.y - triB.y) * p.x + (triB.x - triA.x) * p.y) * Mathf.Sign(area);
			return s >= 0 && t >= 0 && s + t < 2 * Mathf.Abs(area);
		}

		/// <summary>
		/// Determines whether the given 2D triangle is wound in a clockwise order
		/// </summary>
		public static bool TriangleIsClockwise(Vector2 triA, Vector2 triB, Vector2 triC)
		{
			return SignedTriangleArea2D(triA, triB, triC) < 0;
		}

		/// <summary>
		/// Returns a random point inside a triangle in 3D space.
		/// </summary>
		public static Vector3 RandomPointInTriangle(Vector3 triA, Vector3 triB, Vector3 triC, System.Random rng)
		{
			float randA = (float)rng.NextDouble();
			float randB = (float)rng.NextDouble();
			if (randA + randB > 1)
			{
				randA = 1 - randA;
				randB = 1 - randB;
			}
			return triA + (triB - triA) * randA + (triC - triA) * randB;
		}

	}
}