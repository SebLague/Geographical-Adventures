using UnityEngine;

namespace Seb
{
	public static partial class Maths
	{
		/// <summary>
		/// Rotates the point around the axis by the given angle (in radians)
		/// </summary>
		public static Vector3 RotateAroundAxis(Vector3 point, Vector3 axis, float angle)
		{
			return RotateAroundAxis(point, axis, Mathf.Sin(angle), Mathf.Cos(angle));
		}


		/// <summary>
		/// Rotates given vector by the rotation that aligns startDir with endDir
		/// </summary>
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
}
