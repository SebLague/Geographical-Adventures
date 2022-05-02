using UnityEngine;

namespace Seb.Meshing
{
	public static class CubeSphere
	{

		public static SimpleMeshData[] GenerateMeshes(int resolution, int numSubdivisions = 1, float radius = 1)
		{
			SimpleMeshData[] meshes = new SimpleMeshData[6 * numSubdivisions * numSubdivisions];
			Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
			float faceCoveragePerSubFace = 1f / numSubdivisions;
			int meshIndex = 0;

			foreach (Vector3 faceNormal in faceNormals)
			{
				for (int y = 0; y < numSubdivisions; y++)
				{
					for (int x = 0; x < numSubdivisions; x++)
					{
						Vector2 startT = new Vector2(x, y) * faceCoveragePerSubFace;
						Vector2 endT = startT + Vector2.one * faceCoveragePerSubFace;

						meshes[meshIndex] = CreateFace(resolution, faceNormal, startT, endT, radius);
						meshIndex++;
					}
				}
			}

			return meshes;
		}

		static SimpleMeshData CreateFace(int resolution, Vector3 normal, Vector2 startT, Vector2 endT, float radius)
		{
			int numVerts = resolution * resolution;
			int numTris = (resolution - 1) * (resolution - 1) * 6;
			int triIndex = 0;

			Vector3[] vertices = new Vector3[numVerts];
			int[] triangles = new int[numTris];
			Vector4[] uvs = new Vector4[numVerts];
			Vector3[] normals = new Vector3[numVerts];


			Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
			Vector3 axisB = Vector3.Cross(normal, axisA);



			float ty = startT.y;
			float dx = (endT.x - startT.x) / (resolution - 1);
			float dy = (endT.y - startT.y) / (resolution - 1);

			for (int y = 0; y < resolution; y++)
			{
				float tx = startT.x;

				for (int x = 0; x < resolution; x++)
				{
					int i = x + y * resolution;
					Vector3 pointOnUnitCube = normal + (tx - 0.5f) * 2 * axisA + (ty - 0.5f) * 2 * axisB;
					Vector3 pointOnUnitSphere = CubePointToSpherePoint(pointOnUnitCube);

					vertices[i] = pointOnUnitSphere * radius;
					normals[i] = pointOnUnitSphere;
					Vector2 uv = new Vector2(x / (resolution - 1f), y / (resolution - 1f));
					uvs[i] = uv;

					if (x != resolution - 1 && y != resolution - 1)
					{
						triangles[triIndex] = i;
						triangles[triIndex + 1] = i + resolution + 1;
						triangles[triIndex + 2] = i + resolution;

						triangles[triIndex + 3] = i;
						triangles[triIndex + 4] = i + 1;
						triangles[triIndex + 5] = i + resolution + 1;
						triIndex += 6;
					}
					tx += dx;
				}
				ty += dy;
			}
			return new SimpleMeshData(vertices, triangles, normals, uvs, "Sphere Cube Face");
		}

		// From http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
		public static Vector3 CubePointToSpherePoint(Vector3 p)
		{
			float x2 = p.x * p.x / 2;
			float y2 = p.y * p.y / 2;
			float z2 = p.z * p.z / 2;
			float x = p.x * Mathf.Sqrt(1 - y2 - z2 + (p.y * p.y * p.z * p.z) / 3);
			float y = p.y * Mathf.Sqrt(1 - z2 - x2 + (p.x * p.x * p.z * p.z) / 3);
			float z = p.z * Mathf.Sqrt(1 - x2 - y2 + (p.x * p.x * p.y * p.y) / 3);
			return new Vector3(x, y, z);

		}

		public static Vector3 SpherePointToCubePoint(Vector3 p)
		{
			float absX = Mathf.Abs(p.x);
			float absY = Mathf.Abs(p.y);
			float absZ = Mathf.Abs(p.z);

			if (absY >= absX && absY >= absZ)
			{
				return CubifyFace(p);
			}
			else if (absX >= absY && absX >= absZ)
			{
				p = CubifyFace(new Vector3(p.y, p.x, p.z));
				return new Vector3(p.y, p.x, p.z);
			}
			else
			{
				p = CubifyFace(new Vector3(p.x, p.z, p.y));
				return new Vector3(p.x, p.z, p.y);
			}
		}

		// Thanks to http://petrocket.blogspot.com/2010/04/sphere-to-cube-mapping.html
		static Vector3 CubifyFace(Vector3 p)
		{
			const float inverseSqrt2 = 0.70710676908493042f;

			float a2 = p.x * p.x * 2.0f;
			float b2 = p.z * p.z * 2.0f;
			float inner = -a2 + b2 - 3;
			float innersqrt = -Mathf.Sqrt((inner * inner) - 12.0f * a2);

			if (p.x != 0)
			{
				p.x = Mathf.Min(1, Mathf.Sqrt(innersqrt + a2 - b2 + 3.0f) * inverseSqrt2) * Mathf.Sign(p.x);
			}

			if (p.z != 0)
			{
				p.z = Mathf.Min(1, Mathf.Sqrt(innersqrt - a2 + b2 + 3.0f) * inverseSqrt2) * Mathf.Sign(p.z);
			}

			// Top/bottom face
			p.y = Mathf.Sign(p.y);

			return p;
		}


	}
}