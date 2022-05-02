using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Helpers
{
	public static class TransformHelper
	{

		/// <summary> Convert array of transforms into an array of their positions </summary>
		public static Vector3[] GetTransformPositions(Transform[] transforms)
		{
			Vector3[] positions = new Vector3[transforms.Length];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = transforms[i].position;
			}

			return positions;
		}

		/// <summary>
		/// Convert array of transforms into an array of their 2D positions.
		/// The space parameter determines which two axes are used when converting from 3D to 2D.
		/// </summary>
		public static Vector2[] GetTransformPositions2D(Transform[] transforms, Space2D space = Space2D.xy)
		{
			Vector2[] positions = new Vector2[transforms.Length];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = VectorHelper.ToVector2(transforms[i].position, space);
			}

			return positions;
		}

		/// <summary>Returns an array containing the positions of all children of the given parent transform. </summary>
		public static Vector3[] GetAllChildPositions(Transform parent)
		{
			Vector3[] positions = new Vector3[parent.childCount];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = parent.GetChild(i).position;
			}
			return positions;
		}

		/// <summary>
		/// Returns an array containing the 2D positions of all children of the given parent transform.
		/// The space parameter determines which two axes are used when converting from 3D to 2D.
		/// </summary>
		public static Vector2[] GetAllChildPositions2D(Transform parent, Space2D space = Space2D.xy)
		{
			Vector2[] positions = new Vector2[parent.childCount];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = VectorHelper.ToVector2(parent.GetChild(i).position, space);
			}
			return positions;
		}

		/// <summary> Destroys all children of the given parent transform. Works in edit mode as well (use with caution). </summary>
		public static void DestroyAllChildren(Transform parent)
		{
			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				if (Application.isPlaying)
				{
					GameObject.Destroy(parent.GetChild(i).gameObject);
				}
				else
				{
					GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
				}
			}
		}
	}
}
