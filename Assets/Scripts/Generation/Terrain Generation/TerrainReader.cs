using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{
	public class TerrainReader
	{
		public static FaceData[] LoadFromFile(TextAsset terrainFile)
		{
			Debug.Log("Loading terrain from file: " + terrainFile.name);

			byte[] bytes = terrainFile.bytes;
			int[] intValues = new int[bytes.Length / sizeof(int)];
			System.Buffer.BlockCopy(bytes, 0, intValues, 0, bytes.Length);
			Queue<int> data = new Queue<int>(intValues);

			int numFaces = data.Dequeue();
			FaceData[] faceData = new FaceData[numFaces];

			for (int i = 0; i < numFaces; i++)
			{
				int numTriangles = data.Dequeue();
				int[] triangles = ExtractArrayFromQueue(data, numTriangles);
				int numVertexEntries = data.Dequeue();
				int[] pointDataStream = ExtractArrayFromQueue(data, numVertexEntries);
				int numNormalEntries = data.Dequeue();
				int[] normalDataStream = ExtractArrayFromQueue(data, numNormalEntries);

				faceData[i] = new FaceData(pointDataStream, triangles, normalDataStream);
			}

			return faceData;
		}

		// Read specified number of elements from a queue into an array
		static int[] ExtractArrayFromQueue(Queue<int> queue, int length)
		{
			int[] extractedData = new int[length];
			for (int i = 0; i < length; i++)
			{
				extractedData[i] = queue.Dequeue();
			}
			return extractedData;
		}
	}
}