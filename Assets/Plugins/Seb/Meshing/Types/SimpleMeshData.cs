using UnityEngine;
using System.Collections.Generic;

namespace Seb.Meshing
{
	// Class for holding data for a 'simple' mesh.
	// A simple mesh has only vertices, triangles, normals and texCoords
	// (no submeshes, bone weights, or other such fancy things)
	[System.Serializable]
	public class SimpleMeshData
	{
		public string name;

		public int[] triangles = new int[0];
		public Vector3[] vertices = new Vector3[0];
		public Vector3[] normals = new Vector3[0];
		public Vector4[] texCoords = new Vector4[0];

		public SimpleMeshData(string name)
		{
			this.name = name;
		}

		public SimpleMeshData(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector4[] texCoords, string name = "Mesh")
		{
			this.vertices = vertices;
			this.triangles = triangles;
			this.normals = normals;
			this.texCoords = texCoords;
			this.name = name;
		}

		public SimpleMeshData(Vector3[] vertices, int[] triangles, Vector3[] normals, string name = "Mesh")
		{
			this.vertices = vertices;
			this.triangles = triangles;
			this.normals = normals;
			this.name = name;
		}

		public SimpleMeshData(Vector3[] vertices, int[] triangles, string name = "Mesh")
		{
			this.vertices = vertices;
			this.triangles = triangles;
			this.name = name;
		}

		public Mesh ToMesh()
		{
			return MeshHelper.CreateMesh(this);
		}

		public void ToMesh(ref Mesh mesh)
		{
			MeshHelper.CreateMesh(ref mesh, this, false);
		}

		public byte[] ToBytes()
		{
			return MeshSerializer.MeshToBytes(this);
		}

		public static SimpleMeshData FromBytes(byte[] bytes)
		{
			return MeshSerializer.BytesToMesh(bytes);
		}

		/// <summary>
		/// Reorders the vertex and triangle lists for increased rendering efficiency. Also removes redundant (i.e. unindexed) vertices.
		/// </summary>
		public void Optimize()
		{
			Mesh mesh = ToMesh();
			mesh.Optimize();
			// Store optimized mesh data
			vertices = mesh.vertices;
			triangles = mesh.triangles;
			normals = mesh.normals;
			var reorderedUVs = new System.Collections.Generic.List<Vector4>();
			mesh.GetUVs(0, reorderedUVs);
			texCoords = reorderedUVs.ToArray();
		}

		/// <summary>
		/// Removes the specified vertex from the mesh.
		/// Note! this doesn't technically remove the vertex; rather it removes any triangles that reference that vertex.
		/// As such, it's highly recommended to make a call to the Optimize function before using the mesh, to get rid of the redundant vertices.
		/// </summary>
		public void RemoveVertex(int vertexIndex)
		{
			List<int> newTriangles = new List<int>(capacity: triangles.Length);
			int numTriangles = triangles.Length / 3;

			for (int i = 0; i < numTriangles; i++)
			{
				int a = triangles[i * 3 + 0];
				int b = triangles[i * 3 + 1];
				int c = triangles[i * 3 + 2];
				if (a != vertexIndex && b != vertexIndex && c != vertexIndex)
				{
					newTriangles.Add(a);
					newTriangles.Add(b);
					newTriangles.Add(c);
				}
			}

			triangles = newTriangles.ToArray();
		}

		public void RecalculateNormals()
		{
			Mesh mesh = ToMesh();
			mesh.RecalculateNormals();
			this.normals = mesh.normals;
		}

		/// <summary>
		/// Combines the data of another mesh into this mesh data
		/// </summary>
		public void Combine(SimpleMeshData other)
		{
			SimpleMeshData combinedMesh = Combine(this, other);
			this.vertices = combinedMesh.vertices;
			this.triangles = combinedMesh.triangles;
			this.normals = combinedMesh.normals;
			this.texCoords = combinedMesh.texCoords;
		}

		/// <summary>
		/// Combine the data of two simple meshes into one
		/// </summary>
		public static SimpleMeshData Combine(SimpleMeshData a, SimpleMeshData b, string newName = "Combined Mesh")
		{
			Vector3[] combinedVertices = CombineArrays(a.vertices, b.vertices);
			int[] combinedTriangles = new int[a.triangles.Length + b.triangles.Length];

			System.Array.Copy(a.triangles, combinedTriangles, a.triangles.Length);
			for (int i = 0; i < b.triangles.Length; i++)
			{
				combinedTriangles[i + a.triangles.Length] = b.triangles[i] + a.vertices.Length;
			}

			Vector3[] combinedNormals = CombineArraysEnforceLength(a.normals, b.normals, combinedVertices.Length);
			Vector4[] combinedTexCoords = CombineArraysEnforceLength(a.texCoords, b.texCoords, combinedVertices.Length);

			return new SimpleMeshData(combinedVertices, combinedTriangles, combinedNormals, combinedTexCoords, newName);

			T[] CombineArraysEnforceLength<T>(T[] arrayA, T[] arrayB, int length)
			{
				// If both arrays are empty, then return empty array
				if (arrayA.Length == 0 && arrayB.Length == 0)
				{
					return new T[0];
				}
				// If only one array is empty, then pad the other with zeros
				else
				{
					if (arrayA.Length == 0)
					{
						arrayA = new T[length - arrayB.Length];
					}
					else if (arrayB.Length == 0)
					{
						arrayB = new T[length - arrayA.Length];
					}
				}
				return CombineArrays(arrayA, arrayB);
			}

			T[] CombineArrays<T>(T[] arrayA, T[] arrayB)
			{
				T[] combinedArray = new T[arrayA.Length + arrayB.Length];
				System.Array.Copy(arrayA, 0, combinedArray, 0, arrayA.Length);
				System.Array.Copy(arrayB, 0, combinedArray, arrayA.Length, arrayB.Length);
				return combinedArray;
			}
		}
	}
}