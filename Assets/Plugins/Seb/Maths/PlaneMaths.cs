using UnityEngine;

namespace Seb
{
	public static partial class PlaneMaths
	{

		/// <summary>
		/// Given an infinite plane defined by some point that the plane passes through, as well as the normal vector of the plane,
		/// this function returns the nearest point on that plane to the given point p.
		/// </summary>
		public static Vector3 ClosestPointOnPlane(Vector3 anyPointOnPlane, Vector3 planeNormal, Vector3 p)
		{
			float signedDstToPlane = Vector3.Dot(anyPointOnPlane - p, planeNormal);
			Vector3 closestPointOnPlane = p + planeNormal * signedDstToPlane;
			return closestPointOnPlane;
		}
	}
}