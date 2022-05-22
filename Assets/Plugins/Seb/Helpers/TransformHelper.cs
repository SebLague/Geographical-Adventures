using UnityEngine;

namespace Seb
{
	public static class TransformHelper
	{

		/// <summary>
		/// Returns an array of the parent's direct children.
		/// Does not include the childrens' children (and so on).
		/// </summary>
		public static Transform[] GetChildren(Transform parent)
		{
			Transform[] children = new Transform[parent.childCount];
			for (int i = 0; i < children.Length; i++)
			{
				children[i] = parent.GetChild(i);
			}

			return children;
		}

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

		/// <summary>
		/// Destroys all children of the given parent transform.
		/// </summary>
		public static void DestroyAllChildren(Transform parent, bool allowInEditMode = false)
		{
			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				if (Application.isPlaying)
				{
					GameObject.Destroy(parent.GetChild(i).gameObject);
				}
				else if (allowInEditMode)
				{
					GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
				}
			}
		}
	}
}
