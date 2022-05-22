using UnityEngine;

// Some maths relating to vectors

namespace Seb
{
	public static partial class Maths
	{
		/// <summary>
		/// Returns a unit-length vector that is perpendicular to the input vector
		/// </summary>
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

		/// <summary>
		/// Returns two unit-length vectors which are perpendicular both to the input vector and to one another.
		/// </summary>
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

		// Use Vector3.Cross() instead, this is just for reference
		static Vector3 Cross(Vector3 a, Vector3 b)
		{
			float x = a.y * b.z - a.z * b.y;
			float y = a.z * b.x - a.x * b.z;
			float z = a.x * b.y - a.y * b.x;
			return new Vector3(x, y, z);
		}

		// Use Vector3.Dot() instead, this is just for reference
		static float Dot(Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.y * b.y + a.z * b.z;
		}

	}
}
