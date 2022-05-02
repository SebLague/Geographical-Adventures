using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

namespace TerrainGeneration
{
	public class OutlineMeshCreator : Generator
	{

		[Header("Settings")]
		public float radius;
		public int resolution;

		[Header("References")]
		public TextAsset outlineDataFile;
		public Material outlineMat;

		[Header("Save/Load Settings")]
		public string saveFileName;
		public TextAsset loadFile;

		List<Outline> outlines;
		SimpleMeshData[] meshData;

		public bool combine;
		public bool process;

		protected override void Start()
		{
			base.Start();

		}

		public override void StartGenerating()
		{
			NotifyGenerationStarted();
			outlines = new List<Outline>();

			var sw = System.Diagnostics.Stopwatch.StartNew();

			TerrainGenerator.AllOutlines allOutlines = JsonUtility.FromJson<TerrainGenerator.AllOutlines>(outlineDataFile.text);
			foreach (var outline in allOutlines.paths)
			{
				AddOutline(outline.path);
			}
			if (process)
			{
				outlines = ProcessAllOutlines();
			}

			// Create
			if (combine)
			{
				meshData = CreateOutlineMeshes();
			}
			else
			{
				meshData = CreateOutlineMeshesNoGrouping();
			}

			// Display
			for (int i = 0; i < meshData.Length; i++)
			{
				MeshHelper.CreateRendererObject("Outline " + i, meshData[i], outlineMat, transform);
			}

			Debug.Log("Outline creation complete: " + sw.ElapsedMilliseconds + " ms.");

			NotifyGenerationComplete();

		}

		List<Outline> ProcessAllOutlines()
		{
			List<Outline> processedOutlines = new List<Outline>();

			for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex++)
			{
				Outline outline = outlines[outlineIndex];
				bool[] overlapFlags = new bool[outline.NumPoints];
				HashSet<Vector3> outlinePointsHash = new HashSet<Vector3>(outline.points);

				for (int otherIndex = outlineIndex + 1; otherIndex < outlines.Count; otherIndex++)
				{
					Outline other = outlines[otherIndex];
					if (outline.bounds.Overlaps(other.bounds) && outlinePointsHash.Overlaps(other.points))
					{
						for (int i = 0; i < outline.NumPoints; i++)
						{
							for (int j = 0; j < other.NumPoints; j++)
							{
								overlapFlags[i] |= outline.points[i] == other.points[j];
							}
						}
					}
				}
				processedOutlines.AddRange(Extract(outline, overlapFlags));
			}

			return processedOutlines;
		}

		List<Outline> Extract(Outline outline, bool[] overlapFlags)
		{
			List<Outline> extractedOutlines = new List<Outline>();
			// Find starting point where current point is overlapping, but next point is not
			int startIndex = -1;
			bool anyOverlap = false;
			for (int i = 0; i < overlapFlags.Length; i++)
			{
				anyOverlap |= overlapFlags[i];
				if (overlapFlags[i] && !overlapFlags[(i + 1) % outline.NumPoints])
				{
					startIndex = i;
					break;
				}
			}

			if (!anyOverlap)
			{
				extractedOutlines.Add(outline);
			}
			else if (startIndex != -1)
			{
				List<Vector3> extractedPoints = new List<Vector3>();
				bool extracting = false;
				for (int i = 0; i < outline.NumPoints; i++)
				{
					int index = (startIndex + i) % outline.NumPoints;
					int nextIndex = (startIndex + i + 1) % outline.NumPoints;

					if (overlapFlags[index] && !overlapFlags[nextIndex])
					{
						extracting = true;
						extractedPoints.Clear();
					}

					if (extracting)
					{
						extractedPoints.Add(outline.points[index]);
						if (overlapFlags[nextIndex])
						{
							extractedPoints.Add(outline.points[nextIndex]);
							extractedOutlines.Add(new Outline(extractedPoints.ToArray(), false));
							extracting = false;
						}
					}
				}
			}
			return extractedOutlines;
		}

		public override void Load()
		{
			MeshLoader.Load(loadFile, outlineMat, transform, useStaticBatching: false);
		}

		public override void Save()
		{
			byte[] meshBytes = MeshSerializer.MeshesToBytes(meshData);
			FileHelper.SaveBytesToFile(SavePath, saveFileName, meshBytes, log: true);
		}

		void AddOutline(Vector3[] path)
		{
			int numPoints = path.Length;
			if ((path[0] - path[path.Length - 1]).magnitude < 0.01f)
			{
				numPoints = path.Length - 1; // dont include duplicate last point
			}

			Vector3[] points = new Vector3[numPoints];
			for (int i = 0; i < points.Length; i++)
			{
				points[i] = path[i];
			}

			Outline outline = new Outline(points, true);
			outlines.Add(outline);
		}

		SimpleMeshData[] CreateOutlineMeshesNoGrouping()
		{
			SimpleMeshData[] meshes = new SimpleMeshData[outlines.Count];
			for (int i = 0; i < meshes.Length; i++)
			{
				SimpleMeshData outlineMesh = PipeMeshGenerator.GenerateMesh(outlines[i].points, closed: outlines[i].isClosed, radius, resolution);
				meshes[i] = outlineMesh;
			}

			return meshes;
		}

		SimpleMeshData[] CreateOutlineMeshes()
		{
			Bounds3D[] groupBounds = CreateGroupBounds();
			SimpleMeshData[] meshes = new SimpleMeshData[groupBounds.Length];

			for (int i = 0; i < meshes.Length; i++)
			{
				//DebugExtra.DrawBox(groupBounds[i].Centre, groupBounds[i].Size / 2, Color.green, 1000);
				meshes[i] = new SimpleMeshData($"Outline group {i}");
			}

			foreach (Outline outline in outlines)
			{
				int bestBoundsIndex = 0;
				float smallestVolumeIncrease = float.MaxValue;

				for (int i = 0; i < groupBounds.Length; i++)
				{
					Bounds3D combinedBounds = Bounds3D.Combine(groupBounds[i], outline.bounds);
					float volumeIncrease = combinedBounds.Volume - groupBounds[i].Volume;
					if (volumeIncrease < smallestVolumeIncrease)
					{
						smallestVolumeIncrease = volumeIncrease;
						bestBoundsIndex = i;
					}
				}

				SimpleMeshData outlineMesh = PipeMeshGenerator.GenerateMesh(outline.points, closed: outline.isClosed, radius: radius, resolution: resolution);
				meshes[bestBoundsIndex].Combine(outlineMesh);
			}

			for (int i = 0; i < meshes.Length; i++)
			{
				meshes[i].Optimize();
			}

			return meshes;
		}

		Bounds3D[] CreateGroupBounds()
		{
			float heightSum = 0;
			int pointCount = 0;
			foreach (Outline outline in outlines)
			{
				for (int i = 0; i < outline.points.Length; i++)
				{
					heightSum += outline.points[i].magnitude;
					pointCount++;
				}
			}

			float averageHeight = heightSum / pointCount;

			SimpleMeshData[] cubeSphereFaces = CubeSphere.GenerateMeshes(resolution: 10, numSubdivisions: 2, radius: averageHeight);
			Bounds3D[] groupBounds = new Bounds3D[cubeSphereFaces.Length];

			for (int i = 0; i < groupBounds.Length; i++)
			{
				groupBounds[i] = new Bounds3D(cubeSphereFaces[i].vertices);
			}
			return groupBounds;
		}




		class Outline
		{
			public Vector3[] points;
			public Bounds3D bounds;
			public bool isClosed;

			public Outline(Vector3[] points, bool isClosed)
			{
				this.points = points;
				this.bounds = new Bounds3D(points);
				this.isClosed = isClosed;
			}

			public int NumPoints
			{
				get
				{
					return points.Length;
				}
			}
		}
	}
}