using UnityEngine;

/*
 * Processes given arrays of hull and hole points into single array, enforcing correct -wiseness.
 * Also provides convenience methods for accessing different hull/hole points
 */

namespace Seb.MathsHelper.Triangulation
{
	public class Polygon
	{

		public readonly Vector2[] points;
		public readonly int numPoints;

		public readonly int numHullPoints;

		public readonly int[] numPointsPerHole;
		public readonly int numHoles;

		readonly int[] holeStartIndices;

		public Polygon(Vector2[] hull, Vector2[][] holes)
		{
			// Ensure last point is not duplicate of first point (as is standard in some polygon representations)
			RemoveDuplicateEndPoints(ref hull);
			for (int i = 0; i < holes.Length; i++)
			{
				RemoveDuplicateEndPoints(ref holes[i]);
			}

			numHullPoints = hull.Length;
			numHoles = holes.GetLength(0);

			numPointsPerHole = new int[numHoles];
			holeStartIndices = new int[numHoles];
			int numHolePointsSum = 0;

			for (int i = 0; i < holes.GetLength(0); i++)
			{
				numPointsPerHole[i] = holes[i].Length;

				holeStartIndices[i] = numHullPoints + numHolePointsSum;
				numHolePointsSum += numPointsPerHole[i];
			}

			numPoints = numHullPoints + numHolePointsSum;
			points = new Vector2[numPoints];


			// add hull points, ensuring they wind in counterclockwise order
			bool reverseHullPointsOrder = !PointsAreCounterClockwise(hull);
			for (int i = 0; i < numHullPoints; i++)
			{
				points[i] = hull[(reverseHullPointsOrder) ? numHullPoints - 1 - i : i];
			}

			// add hole points, ensuring they wind in clockwise order
			for (int i = 0; i < numHoles; i++)
			{
				bool reverseHolePointsOrder = PointsAreCounterClockwise(holes[i]);
				for (int j = 0; j < holes[i].Length; j++)
				{
					points[IndexOfPointInHole(j, i)] = holes[i][(reverseHolePointsOrder) ? holes[i].Length - j - 1 : j];
				}
			}

		}

		public Polygon(Vector2[] hull) : this(hull, new Vector2[0][])
		{
		}

		bool PointsAreCounterClockwise(Vector2[] testPoints)
		{
			float signedArea = 0;
			for (int i = 0; i < testPoints.Length; i++)
			{
				int nextIndex = (i + 1) % testPoints.Length;
				signedArea += (testPoints[nextIndex].x - testPoints[i].x) * (testPoints[nextIndex].y + testPoints[i].y);
			}

			return signedArea < 0;
		}

		public int IndexOfFirstPointInHole(int holeIndex)
		{
			return holeStartIndices[holeIndex];
		}

		public int IndexOfPointInHole(int index, int holeIndex)
		{
			return holeStartIndices[holeIndex] + index;
		}

		public Vector2 GetHolePoint(int index, int holeIndex)
		{
			return points[holeStartIndices[holeIndex] + index];
		}

		void RemoveDuplicateEndPoints(ref Vector2[] array)
		{
			if ((array[0] - array[array.Length - 1]).sqrMagnitude < 0.001f)
			{
				System.Array.Resize(ref array, array.Length - 1);
			}
		}

	}

}