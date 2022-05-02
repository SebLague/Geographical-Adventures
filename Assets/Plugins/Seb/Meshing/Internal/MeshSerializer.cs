using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Seb.Meshing
{
	public static class MeshSerializer
	{

		/// <summary>
		/// Convert a simple mesh into an array of bytes. Does not support 'fancy' things like submeshes, vertex colors, bone weights etc...
		/// </summary>
		// Format:
		// Total num bytes in data: Int32
		// Name byte length : Int32
		// Name bytes
		// Vertex count: Int32
		// Vertices: 3 floats per vertex
		// Triangle index count (i.e. 3x the number of triangles): Int32
		// Triangle indices: 3x Int32 per triangle
		public static byte[] MeshToBytes(SimpleMeshData meshData)
		{
			MemoryStream stream = new MemoryStream();
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				byte[] nameBytes = System.Text.Encoding.Unicode.GetBytes(meshData.name);
				writer.Write(nameBytes.Length);
				writer.Write(nameBytes);

				WriteVector3Array(writer, meshData.vertices);
				WriteIntArray(writer, meshData.triangles);
				WriteVector3Array(writer, meshData.normals);
				WriteVector4Array(writer, meshData.texCoords);

				// Prepend total byte count to data (presumably there's a better way to do this)
				byte[] allData = new byte[sizeof(int) + writer.BaseStream.Length];
				System.Array.Copy(System.BitConverter.GetBytes(allData.Length), allData, sizeof(int));
				System.Array.Copy(stream.GetBuffer(), 0, allData, sizeof(int), stream.Length);

				return allData;
			}

		}

		public static SimpleMeshData BytesToMesh(byte[] bytes)
		{
			MemoryStream stream = new MemoryStream(bytes);
			using (BinaryReader reader = new BinaryReader(stream))
			{
				int numBytes = reader.ReadInt32();
				Debug.Assert(numBytes == bytes.Length, "Wrong number of bytes in mesh data");
				// Read name
				int nameByteCount = reader.ReadInt32();
				byte[] nameBytes = reader.ReadBytes(nameByteCount);
				string name = System.Text.Encoding.Unicode.GetString(nameBytes);

				Vector3[] vertices = ReadVector3Array(reader);
				int[] triangles = ReadIntArray(reader);
				Vector3[] normals = ReadVector3Array(reader);
				Vector4[] texCoords = ReadVector4Array(reader);


				// Return
				SimpleMeshData meshData = new SimpleMeshData(vertices, triangles, normals);
				meshData.name = name;
				return meshData;
			}
		}

		public static byte[] MeshesToBytes(SimpleMeshData[] meshData)
		{
			List<byte> allBytes = new List<byte>();
			foreach (var m in meshData)
			{
				byte[] bytes = MeshToBytes(m);
				allBytes.AddRange(bytes);
			}
			return allBytes.ToArray();
		}

		public static SimpleMeshData[] BytesToMeshes(byte[] allBytes)
		{
			List<SimpleMeshData> allMeshData = new List<SimpleMeshData>();
			MemoryStream stream = new MemoryStream(allBytes);
			using (BinaryReader reader = new BinaryReader(stream))
			{
				int currentByteIndex = 0;
				while (true)
				{
					int numBytes = reader.ReadInt32();

					byte[] bytes = new byte[numBytes];
					System.Array.Copy(allBytes, currentByteIndex, bytes, 0, numBytes);
					allMeshData.Add(BytesToMesh(bytes));

					currentByteIndex += numBytes;
					reader.BaseStream.Seek(currentByteIndex, SeekOrigin.Begin);
					if (reader.BaseStream.Position == reader.BaseStream.Length || numBytes == 0)
					{
						break;
					}
				}

			}
			return allMeshData.ToArray();
		}


		static void WriteIntArray(BinaryWriter writer, int[] ints)
		{
			writer.Write(ints.Length);

			byte[] bytes = new byte[ints.Length * sizeof(int)];
			System.Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
			writer.Write(bytes);
		}


		static void WriteVector3Array(BinaryWriter writer, Vector3[] vectors)
		{
			writer.Write(vectors.Length);
			for (int i = 0; i < vectors.Length; i++)
			{
				Vector3 v = vectors[i];
				writer.Write(v.x);
				writer.Write(v.y);
				writer.Write(v.z);
			}
		}

		static void WriteVector4Array(BinaryWriter writer, Vector4[] vectors)
		{
			writer.Write(vectors.Length);
			for (int i = 0; i < vectors.Length; i++)
			{
				Vector4 v = vectors[i];
				writer.Write(v.x);
				writer.Write(v.y);
				writer.Write(v.z);
				writer.Write(v.w);
			}
		}

		static int[] ReadIntArray(BinaryReader reader)
		{
			int numInts = reader.ReadInt32();
			int[] ints = new int[numInts];
			byte[] bytes = reader.ReadBytes(numInts * sizeof(int));
			System.Buffer.BlockCopy(bytes, 0, ints, 0, bytes.Length);

			return ints;
		}

		static Vector3[] ReadVector3Array(BinaryReader reader)
		{
			Vector3[] vectors = new Vector3[reader.ReadInt32()];
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
			}
			return vectors;
		}

		static Vector4[] ReadVector4Array(BinaryReader reader)
		{
			Vector4[] vectors = new Vector4[reader.ReadInt32()];
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i] = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
			}
			return vectors;
		}

	}
}