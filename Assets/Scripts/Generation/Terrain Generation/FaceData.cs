using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainGeneration
{
	[System.Serializable]
	public struct FaceData
	{
		// Points (normalized), w = elevation
		public readonly Vector4[] pointData;
		public readonly Vector3[] normals;
		public readonly int[] triangles;
		// Each set of 4 ints represents x,y,z,elevation
		// Used for easy saving and loading
		public readonly int[] pointDataStream;
		public readonly int[] normalDataStream;
		const int numDecimalPlaces = 6;

		public FaceData(Vector3[] spherePoints, float[] heights, int[] triangles, Vector3[] normals)
		{
			this.triangles = triangles;
			this.normals = normals;

			pointData = new Vector4[spherePoints.Length];
			for (int i = 0; i < pointData.Length; i++)
			{
				Vector3 spherePoint = spherePoints[i];
				pointData[i] = new Vector4(spherePoint.x, spherePoint.y, spherePoint.z, heights[i]);
			}

			int precision = (int)Mathf.Pow(10, numDecimalPlaces);

			// Create point data stream
			pointDataStream = new int[pointData.Length * 4];

			for (int i = 0; i < pointData.Length; i++)
			{
				pointDataStream[i * 4 + 0] = (int)(pointData[i].x * precision);
				pointDataStream[i * 4 + 1] = (int)(pointData[i].y * precision);
				pointDataStream[i * 4 + 2] = (int)(pointData[i].z * precision);
				pointDataStream[i * 4 + 3] = (int)(pointData[i].w * precision);

			}
			// Create normal data stream
			normalDataStream = new int[normals.Length * 3];
			for (int i = 0; i < normals.Length; i++)
			{
				normalDataStream[i * 3 + 0] = (int)(normals[i].x * precision);
				normalDataStream[i * 3 + 1] = (int)(normals[i].y * precision);
				normalDataStream[i * 3 + 2] = (int)(normals[i].z * precision);
			}
		}

		public FaceData(int[] pointDataStream, int[] triangles, int[] normalDataStream)
		{
			this.pointDataStream = pointDataStream;
			this.triangles = triangles;
			this.normalDataStream = normalDataStream;

			float precision = Mathf.Pow(10, numDecimalPlaces);

			// Create vertices from vertex stream
			pointData = new Vector4[pointDataStream.Length / 4];

			for (int i = 0; i < pointData.Length; i++)
			{
				float x = pointDataStream[i * 4 + 0] / precision;
				float y = pointDataStream[i * 4 + 1] / precision;
				float z = pointDataStream[i * 4 + 2] / precision;
				float w = pointDataStream[i * 4 + 3] / precision;
				pointData[i] = new Vector4(x, y, z, w);
			}

			// Create normals from normal stream
			normals = new Vector3[normalDataStream.Length / 3];

			for (int i = 0; i < normals.Length; i++)
			{
				float x = normalDataStream[i * 3 + 0] / precision;
				float y = normalDataStream[i * 3 + 1] / precision;
				float z = normalDataStream[i * 3 + 2] / precision;
				normals[i] = new Vector3(x, y, z);
			}

		}

		public Mesh CreateMesh(float worldRadius, float heightMultiplier)
		{
			Vector3[] vertices = new Vector3[pointData.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				float elevation = worldRadius + pointData[i].w * heightMultiplier;
				vertices[i] = (pointData[i]) * elevation;
			}

			Mesh mesh = new Mesh();
			mesh.indexFormat = (vertices.Length < (1 << 16)) ? IndexFormat.UInt16 : IndexFormat.UInt32;
			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0, true);
			//mesh.RecalculateNormals();
			mesh.normals = normals;
			mesh.name = "Terrain Mesh Chunk";
			mesh.UploadMeshData(markNoLongerReadable: true);
			return mesh;
		}



	}
}
