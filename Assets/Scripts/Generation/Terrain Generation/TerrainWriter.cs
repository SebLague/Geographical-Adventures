using UnityEngine;
using System.IO;

namespace TerrainGeneration
{
	public class TerrainWriter
	{
		public static void WriteToFile(FaceData[] allFaceData, string saveFileName)
		{
			string path = System.IO.Path.Combine("Assets", "Data", "Terrain Mesh", saveFileName + ".bytes");
			Debug.Log("Saving terrain data to: " + path);

			using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
			{
				writer.Write(allFaceData.Length);
				foreach (var faceData in allFaceData)
				{
					byte[] triangleBuffer = new byte[faceData.triangles.Length * sizeof(int)];
					byte[] pointBuffer = new byte[faceData.pointDataStream.Length * sizeof(int)];
					byte[] normalBuffer = new byte[faceData.normalDataStream.Length * sizeof(int)];
					System.Buffer.BlockCopy(faceData.pointDataStream, 0, pointBuffer, 0, pointBuffer.Length);
					System.Buffer.BlockCopy(faceData.triangles, 0, triangleBuffer, 0, triangleBuffer.Length);
					System.Buffer.BlockCopy(faceData.normalDataStream, 0, normalBuffer, 0, normalBuffer.Length);

					writer.Write(faceData.triangles.Length);
					writer.Write(triangleBuffer);
					writer.Write(faceData.pointDataStream.Length);
					writer.Write(pointBuffer);
					writer.Write(faceData.normalDataStream.Length);
					writer.Write(normalBuffer);

					
				}

			}
		}

	}
}