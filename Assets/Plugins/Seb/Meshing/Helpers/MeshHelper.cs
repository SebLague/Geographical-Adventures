using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seb.Meshing
{

	public static class MeshHelper
	{

		static Material defaultMaterial;
		static Shader defaultShader;

		public static Mesh CreateMesh(SimpleMeshData meshData, bool recalculateNormals = false)
		{
			Mesh mesh = new Mesh();
			CreateMesh(ref mesh, meshData, recalculateNormals);
			return mesh;
		}

		public static void CreateMesh(ref Mesh mesh, SimpleMeshData meshData, bool recalculateNormals = false)
		{
			if (mesh == null)
			{
				mesh = new Mesh();
			}
			else
			{
				mesh.Clear();
			}

			mesh.name = meshData.name;

			int numVerts = meshData.vertices.Length;
			mesh.indexFormat = GetMeshIndexFormat(numVerts);

			mesh.SetVertices(meshData.vertices);
			mesh.SetTriangles(meshData.triangles, submesh: 0, calculateBounds: true);
			if (recalculateNormals)
			{
				mesh.RecalculateNormals();
			}
			else if (meshData.normals.Length == numVerts)
			{
				mesh.SetNormals(meshData.normals);
			}

			if (meshData.texCoords.Length == numVerts)
			{
				mesh.SetUVs(0, meshData.texCoords);
			}
		}

		public static Mesh CreateMesh(Vector3[] vertices, int[] triangles, bool recalculateNormals = false)
		{
			SimpleMeshData meshData = new SimpleMeshData(vertices, triangles);
			return CreateMesh(meshData, recalculateNormals);
		}


		public static Mesh CreateMesh(Vector3[] vertices, int[] triangles, Vector3[] normals)
		{
			SimpleMeshData meshData = new SimpleMeshData(vertices, triangles, normals);
			return CreateMesh(meshData, false);
		}


		public static Mesh CreateMesh(Vector2[] vertices2D, int[] triangles, bool recalculateNormals = false)
		{
			Vector3[] vertices = new Vector3[vertices2D.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = vertices2D[i];
			}
			return CreateMesh(vertices, triangles, recalculateNormals);
		}


		public static IndexFormat GetMeshIndexFormat(int numVerts)
		{
			const int maxVertCount16bit = (1 << 16) - 1;
			return (numVerts <= maxVertCount16bit) ? IndexFormat.UInt16 : IndexFormat.UInt32;
		}

		// Note, triangle face dir depends on order of outline points and on the direction
		// (TODO: handle face dir automatically)
		public static Mesh CreateEdgeMesh(Vector3[] outline, Vector3 direction, float dst)
		{
			int numFaces = outline.Length;
			Vector3[] vertices = new Vector3[outline.Length * 2];
			int[] triangles = new int[numFaces * 2 * 3];

			for (int i = 0; i < outline.Length; i++)
			{
				int topVertexIndex = i * 2 + 0;
				int bottomVertexIndex = i * 2 + 1;
				vertices[topVertexIndex] = outline[i];
				vertices[bottomVertexIndex] = outline[i] + direction * dst;

				int triIndex = i * 2 * 3;
				triangles[triIndex + 0] = topVertexIndex;
				triangles[triIndex + 1] = bottomVertexIndex;
				triangles[triIndex + 2] = (bottomVertexIndex + 1) % vertices.Length;

				triangles[triIndex + 3] = bottomVertexIndex;
				triangles[triIndex + 4] = (bottomVertexIndex + 2) % vertices.Length;
				triangles[triIndex + 5] = (bottomVertexIndex + 1) % vertices.Length;
			}
			return CreateMesh(vertices, triangles, true);
		}

		// Create GameObject with mesh renderer and filter components applied
		public static RenderObject CreateRendererObject(string name, Mesh mesh = null, Material material = null, Transform parent = null, int layer = 0)
		{
			GameObject meshHolder = new GameObject(name);
			MeshFilter meshFilter = meshHolder.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = meshHolder.AddComponent<MeshRenderer>();



			if (material == null)
			{
				material = GetDefaultMaterial();
			}

			if (Application.isPlaying)
			{
				meshFilter.mesh = mesh;
				meshRenderer.material = material;
			}
			else
			{
				meshFilter.sharedMesh = mesh;
				meshRenderer.sharedMaterial = material;
			}

			meshHolder.transform.parent = parent;
			meshHolder.layer = layer;

			RenderObject renderObject = new RenderObject(meshHolder, meshRenderer, meshFilter, material);
			return renderObject;
		}

		public static RenderObject CreateRendererObject(string name, SimpleMeshData meshData, Material material = null, Transform parent = null, int layer = 0)
		{
			Mesh mesh = CreateMesh(meshData);
			return CreateRendererObject(name, mesh, material, parent, layer);
		}

		static Material GetDefaultMaterial()
		{
			if (defaultShader == null)
			{
				defaultShader = Shader.Find("Unlit/Color");
			}
			if (defaultMaterial == null || defaultMaterial.shader != defaultShader)
			{
				defaultMaterial = new Material(defaultShader);
			}

			return defaultMaterial;
		}
	}
}