using UnityEngine;
using System.Collections.Generic;
using Seb.MathsHelper;

// A collection of maths functions.
namespace Maths
{

	public static class Triangle
	{

		public static float Area(Vector3 a, Vector3 b, Vector3 c)
		{
			// https://math.stackexchange.com/a/1951650
			Vector3 ortho = Vector3.Cross(c - a, b - a);
			float parallogramArea = ortho.magnitude;
			return parallogramArea * 0.5f;
		}

		public static Vector3 RandomPointInside(Vector3 a, Vector3 b, Vector3 c, System.Random prng)
		{
			float randA = (float)prng.NextDouble();
			float randB = (float)prng.NextDouble();
			if (randA + randB > 1)
			{
				randA = 1 - randA;
				randB = 1 - randB;
			}

			return a + (b - a) * randA + (c - a) * randB;
		}

		public static bool Contains(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
			float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
			float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
			return s >= 0 && t >= 0 && (s + t) <= 1;
		}

		public static bool IsClockwise(Vector2 a, Vector2 b, Vector2 c)
		{
			return (c.x - a.x) * (-b.y + a.y) + (c.y - a.y) * (b.x - a.x) < 0;
		}
	}

	public static class Plane
	{
		public static Vector3 ClosestPointOnPlane(Vector3 point, Vector3 pointOnPlane, Vector3 planeNormal)
		{
			float signedDstToPlane = Vector3.Dot(pointOnPlane - point, planeNormal);
			Vector3 closestPointOnPlane = point + planeNormal * signedDstToPlane;
			return closestPointOnPlane;
		}
	}

	public static class Sphere
	{
		// Get n points distributed (fairly) evenly on a sphere (fibonacci sphere)
		public static Vector3[] GetPoints(int numPoints, float radius = 1)
		{
			Vector3[] points = new Vector3[numPoints];
			const double goldenRatio = 1.618033988749894; // (1 + sqrt(5)) / 2
			const double angleIncrement = System.Math.PI * 2 * goldenRatio;

			System.Threading.Tasks.Parallel.For(0, numPoints, i =>
			{
				double t = (double)i / numPoints;
				double inclination = System.Math.Acos(1 - 2 * t);
				double azimuth = angleIncrement * i;

				double x = System.Math.Sin(inclination) * System.Math.Cos(azimuth);
				double y = System.Math.Sin(inclination) * System.Math.Sin(azimuth);
				double z = System.Math.Cos(inclination);
				points[i] = new Vector3((float)x, (float)y, (float)z) * radius;
			});
			return points;
		}

		public static Vector3 RandomPoint(System.Random prng)
		{
			float x = Random.Gaussian(0, 1, prng);
			float y = Random.Gaussian(0, 1, prng);
			float z = Random.Gaussian(0, 1, prng);
			return new Vector3(x, y, z).normalized;
		}


		// Shortest arc distance between two points on a sphere. Points must be normalized.
		public static float DistanceBetweenPointsOnUnitSphere(Vector3 a, Vector3 b)
		{
			// Thanks to https://www.movable-type.co.uk/scripts/latlong-vectors.html
			return Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));
			// This simpler approach works as well, but is less precise for small angles:
			//return Mathf.Acos(Vector3.Dot(a, b));
		}

		public static float DistanceBetweenPointsOnSphere(Vector3 a, Vector3 b, float radius)
		{
			return DistanceBetweenPointsOnUnitSphere(a.normalized, b.normalized) * radius;
		}

		public static bool ContainsPoint(Vector3 sphereCentre, float radius, Vector3 point)
		{
			return (point - sphereCentre).sqrMagnitude <= radius * radius;
		}

		public static bool OverlapsSphere(Vector3 centreA, float radiusA, Vector3 centreB, float radiusB)
		{
			return ContainsPoint(centreA, radiusA + radiusB, centreB);
		}

	}

	public static class Polygon
	{
		/// <summary>
		/// Test if polygon contains given point.
		/// Points can be ordered clockwise or counterclockwise.
		/// Last point in polygon is NOT expected to be a duplicate of the first point.
		/// </summary>
		public static bool ContainsPoint(Vector2 p, Vector2[] points)
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

		public static bool IsClockwise(Vector2[] points)
		{
			return SignedArea(points) > 0;
		}

		public static float Area(Vector2[] points)
		{
			return Mathf.Abs(SignedArea(points));
		}

		public static float SignedArea(Vector2[] points)
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

		public static Vector2[] RandomPointsInside(Vector2[] polygon, int numPoints)
		{
			return new Seb.MathsHelper.PolygonPointGenerator(polygon).GetRandomPoints(numPoints);
		}

		public static Vector2[] RandomPointsInside(Vector2[] polygon, int numPoints, int seed)
		{
			var gen = new Seb.MathsHelper.PolygonPointGenerator(polygon);
			gen.SetSeed(seed);
			return gen.GetRandomPoints(numPoints);
		}

		public static int[] Triangulate(Vector2[] polygon)
		{
			return Seb.MathsHelper.Triangulation.Triangulator.Triangulate(polygon);
		}

	}

	public static class LineSegment
	{

		public static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
		{
			Vector2 aB = b - a;
			Vector2 aP = p - a;
			float sqrLenAB = aB.sqrMagnitude;
			// Handle case where A and B are in same position (so line segment is just a single point)
			if (sqrLenAB == 0)
			{
				return a;
			}

			float t = Mathf.Clamp01(Vector2.Dot(aP, aB) / sqrLenAB);
			return a + aB * t;
		}

		// Sqr distance from point P to line segment AB
		public static float SqrDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
		{
			Vector2 nearestPoint = ClosestPointOnSegment(p, a, b);
			return (p - nearestPoint).sqrMagnitude;
		}

		// Distance from point P to line segment AB
		public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
		{
			return Mathf.Sqrt(SqrDistanceToSegment(p, a, b));
		}

	}

	public static class Line
	{

	}



	public static class Rotation
	{

		/// <summary>
		/// Rotates the point around the axis by the given angle (in radians)
		/// </summary>
		public static Vector3 RotateAroundAxis(Vector3 point, Vector3 axis, float angle)
		{
			return RotateAroundAxis(point, axis, Mathf.Sin(angle), Mathf.Cos(angle));
		}


		// Rotates given vector by the rotation that aligns startDir with endDir
		public static Vector3 RotateBetweenDirections(Vector3 vector, Vector3 startDir, Vector3 endDir)
		{
			Vector3 rotationAxis = Vector3.Cross(startDir, endDir);
			float sinAngle = rotationAxis.magnitude;
			float cosAngle = Vector3.Dot(startDir, endDir);

			return RotateAroundAxis(vector, rotationAxis.normalized, cosAngle, sinAngle);
			// Note: this achieves the same as doing: 
			// return Quaternion.FromToRotation(originalDir, newDir) * point;
		}

		static Vector3 RotateAroundAxis(Vector3 point, Vector3 axis, float sinAngle, float cosAngle)
		{
			// Rodrigues' rotation formula: https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
			return point * cosAngle + Vector3.Cross(axis, point) * sinAngle + axis * Vector3.Dot(axis, point) * (1 - cosAngle);
		}

	}

	public static class Random
	{

		public static float Gaussian(float mean, float standardDeviation, System.Random prng)
		{
			// https://stackoverflow.com/a/6178290
			float theta = 2 * Mathf.PI * (float)prng.NextDouble();
			float rho = Mathf.Sqrt(-2 * Mathf.Log((float)prng.NextDouble()));
			float scale = standardDeviation * rho;
			return mean + scale * Mathf.Cos(theta);
		}

		public static Vector3 RandomPointOnSphere(System.Random prng)
		{
			return Sphere.RandomPoint(prng);
		}

		public static void ShuffleArray<T>(T[] array, System.Random prng)
		{
			// wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
			for (int i = 0; i < array.Length - 1; i++)
			{
				int randomIndex = prng.Next(i, array.Length);
				(array[randomIndex], array[i]) = (array[i], array[randomIndex]);
			}
		}

		/// Pick random index, weighted by the weights array.
		/// For example, if the array contains {1, 6, 3}...
		/// The possible indices would be (0, 1, 2)
		/// and the probabilities for these would be (1/10, 6/10, 3/10)
		public static int WeightedRandomIndex(System.Random prng, float[] weights)
		{
			float weightSum = 0;
			for (int i = 0; i < weights.Length; i++)
			{
				weightSum += weights[i];
			}

			float randomValue = (float)prng.NextDouble() * weightSum;
			float cumul = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				cumul += weights[i];
				if (randomValue < cumul)
				{
					return i;
				}
			}

			return weights.Length - 1;
		}
	}

	public static class Sorting
	{

		/// <summary>
		/// Sorts the given array by the score values (from high to low).
		/// Note, the scores array will be sorted as well in the process.
		/// </summary>
		public static void SortByScores<T>(T[] array, int[] scores)
		{
			Debug.Assert(array.Length == scores.Length, "Cannot sort if array length does not match score length");

			for (int i = 0; i < array.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					if (scores[swapIndex] < scores[j])
					{
						(array[j], array[swapIndex]) = (array[swapIndex], array[j]);
						(scores[j], scores[swapIndex]) = (scores[swapIndex], scores[j]);
					}
				}
			}
		}

		public static void Sort<T>(T[] array, System.Func<T, T, int> compare)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int relativeScore = compare(array[swapIndex], array[j]);
					if (relativeScore < 0)
					{
						(array[j], array[swapIndex]) = (array[swapIndex], array[j]);
					}
				}
			}
		}

		public static void SortPointsByAngle(Vector2[] points, bool clockwise = true)
		{
			Bounds2D bounds = new Bounds2D(points);
			Vector2 centre = bounds.Centre;

			if (clockwise)
			{
				Sort(points, (a, b) => Compare(b, a, centre));
			}
			else
			{
				Sort(points, (a, b) => Compare(a, b, centre));
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

		public static void ShuffleArray<T>(T[] array, System.Random prng)
		{
			Random.ShuffleArray(array, prng);
		}

	}

	public static class Ease
	{

		public static class Cubic
		{
			public static float In(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Pow(t, 3);
			}

			public static float Out(float t)
			{
				t = Mathf.Clamp01(t);
				return 1 - Mathf.Pow(1 - t, 3);
			}

			public static float InOut(float t)
			{
				t = Mathf.Clamp01(t);
				return (t < 0.5f) ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
			}
		}
		public class Quadratic
		{

			public static float In(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Pow(t, 2);
			}

			public static float Out(float t)
			{
				t = Mathf.Clamp01(t);
				return 1 - (1 - t) * (1 - t);
			}

			public static float InOut(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Lerp(In(t), Out(t), t);
			}
		}

		public class Circular
		{
			public static float InOut(float t)
			{
				t = Mathf.Clamp01(t);
				return Mathf.Sqrt(1 - (1 - t) * (1 - t));
			}
		}
	}

	public static class Vector
	{


		public static Vector3 CreateOrthonormalVector(Vector3 v)
		{
			v = v.normalized;
			// Thanks to https://jcgt.org/published/0006/01/01/
			if (v.z < 0)
			{
				float a = 1.0f / (1.0f - v.z);
				float b = v.x * v.y * a;
				return new Vector3(1.0f - v.x * v.x * a, -b, v.x);
			}
			else
			{
				float a = 1.0f / (1.0f + v.z);
				float b = -v.x * v.y * a;
				return new Vector3(1.0f - v.x * v.x * a, b, -v.x);
			}
		}


		public static (Vector3, Vector3) CreateOrthonormalVectors(Vector3 v)
		{
			v = v.normalized;
			// Thanks to https://jcgt.org/published/0006/01/01/
			if (v.z < 0)
			{
				float a = 1.0f / (1.0f - v.z);
				float b = v.x * v.y * a;
				Vector3 orthoA = new Vector3(1.0f - v.x * v.x * a, -b, v.x);
				Vector3 orthoB = new Vector3(b, v.y * v.y * a - 1.0f, -v.y);
				return (orthoA, orthoB);
			}
			else
			{
				float a = 1.0f / (1.0f + v.z);
				float b = -v.x * v.y * a;
				Vector3 orthoA = new Vector3(1.0f - v.x * v.x * a, b, -v.x);
				Vector3 orthoB = new Vector3(b, 1.0f - v.y * v.y * a, -v.y);
				return (orthoA, orthoB);
			}
		}


		public static Vector3 Cross(Vector3 a, Vector3 b)
		{
			float x = a.y * b.z - a.z * b.y;
			float y = a.z * b.x - a.x * b.z;
			float z = a.x * b.y - a.y * b.x;
			return new Vector3(x, y, z);
		}

		public static float Dot(Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.y * b.y + a.z * b.z;
		}
	}

	public struct Other
	{
		// Thanks to https://stackoverflow.com/a/41766138
		public static int GreatestCommonDivisor(int a, int b)
		{
			while (a != 0 && b != 0)
			{
				if (a > b)
					a %= b;
				else
					b %= a;
			}

			return a | b;
		}
	}

}
