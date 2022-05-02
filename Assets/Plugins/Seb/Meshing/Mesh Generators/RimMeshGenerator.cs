using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Meshing
{
	public static class RimMeshGenerator
	{

		// Note, triangle face dir depends on order of outline points and on the direction
		// (TODO: handle face dir automatically)
		public static SimpleMeshData Generate(Vector3[] outline, Vector3 upDirection, float length)
		{
			return Generate(outline, length, (pos) => upDirection);
		}

		public static SimpleMeshData GenerateOnSphere(Vector3[] outline, float length)
		{
			return Generate(outline, length, (pos) => pos.normalized);
		}


		static SimpleMeshData Generate(Vector3[] outline, float length, System.Func<Vector3, Vector3> getUpDirection)
		{
			int numFaces = outline.Length;
			Vector3[] vertices = new Vector3[outline.Length * 2];
			int[] triangles = new int[numFaces * 2 * 3];

			for (int i = 0; i < outline.Length; i++)
			{
				int topVertexIndex = i * 2 + 0;
				int bottomVertexIndex = i * 2 + 1;
				vertices[topVertexIndex] = outline[i];
				vertices[bottomVertexIndex] = outline[i] - getUpDirection(outline[i]) * length;

				int triIndex = i * 2 * 3;
				triangles[triIndex + 0] = topVertexIndex;
				triangles[triIndex + 1] = bottomVertexIndex;
				triangles[triIndex + 2] = (bottomVertexIndex + 1) % vertices.Length;

				triangles[triIndex + 3] = bottomVertexIndex;
				triangles[triIndex + 4] = (bottomVertexIndex + 2) % vertices.Length;
				triangles[triIndex + 5] = (bottomVertexIndex + 1) % vertices.Length;
			}

			return new SimpleMeshData(vertices, triangles);
		}

	}
}