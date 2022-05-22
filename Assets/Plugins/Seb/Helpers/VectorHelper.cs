using UnityEngine;

namespace Seb
{
	public enum Space2D { xy, xz, yz }

	public static class VectorHelper
	{

		/// <summary> Converts an array of 2D vectors to an array of 3D vectors. An optional z value can be given (0 by default). </summary>
		public static Vector3[] To3DArray(Vector2[] array2D, float z = 0)
		{
			Vector3[] array3D = new Vector3[array2D.Length];

			for (int i = 0; i < array3D.Length; i++)
			{
				array3D[i] = new Vector3(array2D[i].x, array2D[i].y, z);
			}

			return array3D;
		}

		/// <summary> Converts an array of Vector3s to an array of Vector2s. The space parameter determines which two axes are used </summary>
		public static Vector2[] To2DArray(Vector3[] array3D, Space2D space = Space2D.xy)
		{
			Vector2[] array2D = new Vector2[array3D.Length];

			for (int i = 0; i < array2D.Length; i++)
			{
				array2D[i] = ToVector2(array3D[i], space);
			}

			return array2D;
		}

		/// <summary> Converts a Vector3 to a Vector2. The space parameter determines which two axes are used </summary>
		public static Vector2 ToVector2(Vector3 p, Space2D space = Space2D.xy)
		{
			float x = p.x;
			float y = p.y;
			if (space == Space2D.xz)
			{
				y = p.z;
			}
			else if (space == Space2D.yz)
			{
				x = p.z;
			}
			return new Vector2(x, y);
		}
	}
}