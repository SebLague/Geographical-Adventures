using UnityEngine;

// Some maths relating to lines and line segments
namespace Seb
{
	public static partial class Maths
	{

		/// <summary>
		/// Returns the closest point on the line segment to the given point
		/// </summary>
		public static Vector3 ClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 p)
		{
			Vector3 aB = lineEnd - lineStart;
			Vector3 aP = p - lineStart;
			float sqrLenAB = aB.sqrMagnitude;
			// Handle case where start/end points are in same position (i.e. line segment is just a single point)
			if (sqrLenAB == 0)
			{
				return lineStart;
			}

			float t = Mathf.Clamp01(Vector3.Dot(aP, aB) / sqrLenAB);
			return lineStart + aB * t;
		}

		/// <summary>
		/// Returns the square of the distance from the given point to the line segment
		/// </summary>
		public static float SqrDistanceToLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 p)
		{
			Vector3 nearestPoint = ClosestPointOnLineSegment(lineStart, lineEnd, p);
			return (p - nearestPoint).sqrMagnitude;
		}

		/// <summary>
		/// Returns the distance from the given point to the line segment
		/// </summary>
		public static float DistanceToLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 p)
		{
			return Mathf.Sqrt(SqrDistanceToLineSegment(lineStart, lineEnd, p));
		}

	}
}