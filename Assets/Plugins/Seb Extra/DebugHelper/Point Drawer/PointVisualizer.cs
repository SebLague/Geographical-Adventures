using UnityEngine;
using System.Collections.Generic;
using Seb.Meshing;

namespace DebugHelper
{
	public class PointVisualizer : MonoBehaviour
	{

		const string shaderName = "DebugHelper/InstancedUnlitPoint";

		public float sizeMultiplier = 1;
		public Shader instanceShader;

		List<Draw> drawList;
		Bounds bounds;
		Mesh mesh;

		void Init()
		{
			if (drawList == null)
			{
				drawList = new List<Draw>();
			}

			IcoSphere.Generate(2).ToMesh(ref mesh);
			bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

		}

		public void AddPoints(Vector2[] points, Color colour, float size = 1)
		{
			if (points.Length > 0)
			{
				var pointBuffer = ComputeHelper.CreateStructuredBuffer(points);
				AddPoints(pointBuffer, colour, isAppendBuffer: false, is3D: false, size);
			}
		}

		public void AddPoints(Vector3[] points, Color colour, float size = 1)
		{
			if (points.Length > 0)
			{
				var pointBuffer = ComputeHelper.CreateStructuredBuffer(points);
				AddPoints(pointBuffer, colour, isAppendBuffer: false, is3D: true, size);
			}
		}

		public void AddPoints(ComputeBuffer pointsBuffer, Color colour, bool isAppendBuffer, bool is3D, float size)
		{
			Init();
			Draw draw = new Draw();
			if (isAppendBuffer)
			{
				draw.argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, pointsBuffer);
			}
			else
			{
				draw.argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, pointsBuffer.count);
			}
			draw.pointBuffer = pointsBuffer;

			Material material = new Material(instanceShader);
			if (is3D)
			{
				material.EnableKeyword("Use3DPoints");
			}
			else
			{
				material.DisableKeyword("Use3DPoints");
			}
			material.SetBuffer("Points", draw.pointBuffer);
			material.SetColor("colour", colour);
			draw.material = material;
			draw.size = size;


			drawList.Add(draw);
		}



		void Update()
		{
			if (drawList != null)
			{
				foreach (var draw in drawList)
				{
					draw.material.SetFloat("sizeMultiplier", draw.size * sizeMultiplier);
					Graphics.DrawMeshInstancedIndirect(mesh, 0, draw.material, bounds, draw.argsBuffer);
				}
			}

		}

		void OnDestroy()
		{
			if (drawList != null)
			{
				foreach (var draw in drawList)
				{
					ComputeHelper.Release(draw.pointBuffer, draw.argsBuffer);
				}
			}

		}

		void OnValidate()
		{
			if (instanceShader == null)
			{
				instanceShader = Shader.Find(shaderName);
			}
		}

		public struct Draw
		{
			public ComputeBuffer pointBuffer;
			public ComputeBuffer argsBuffer;
			public float size;
			public Material material;
		}

	}
}