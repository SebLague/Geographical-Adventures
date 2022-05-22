using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Some maths relating to spheres

namespace Seb
{
	public static partial class Maths
	{

		public static bool SphereContainsPoint(Vector3 sphereCentre, float sphereRadius, Vector3 point)
		{
			return (point - sphereCentre).sqrMagnitude <= sphereRadius * sphereRadius;
		}

		public static bool SphereOverlapsSphere(Vector3 centreA, float radiusA, Vector3 centreB, float radiusB)
		{
			return SphereContainsPoint(centreA, radiusA + radiusB, centreB);
		}

		public static Vector3 RandomPointInSphere(System.Random rng)
		{
			float x = RandomNormal(rng, 0, 1);
			float y = RandomNormal(rng, 0, 1);
			float z = RandomNormal(rng, 0, 1);
			return new Vector3(x, y, z).normalized;
		}

		/// <summary>
		/// Returns the length of the shortest arc between two points on the surface of a unit sphere.
		/// </summary>
		public static float ArcLengthBetweenPointsOnUnitSphere(Vector3 a, Vector3 b)
		{
			// Thanks to https://www.movable-type.co.uk/scripts/latlong-vectors.html
			return Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));
			// Note: The following approach works as well, but is less precise for small angles:
			// return Mathf.Acos(Vector3.Dot(a, b));
		}

		/// <summary>
		/// Returns the length of the shortest arc between two points on the surface of a sphere with the specified radius.
		/// </summary>
		public static float ArcLengthBetweenPointsOnSphere(Vector3 a, Vector3 b, float sphereRadius)
		{
			return ArcLengthBetweenPointsOnUnitSphere(a.normalized, b.normalized) * sphereRadius;
		}

		/// <summary>
		/// Returns n points distributed reasonably evenly on a sphere.
		/// Uses fibonacci spiral technique.
		/// </summary>
		public static Vector3[] GetPointsOnSphereSurface(int numPoints, float radius = 1)
		{
			// Thanks to https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere/44164075#44164075
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
	}
}