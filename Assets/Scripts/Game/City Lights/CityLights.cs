using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityLights : MonoBehaviour
{
	public bool enableCityLights = true;
	public TerrainGeneration.TerrainHeightSettings heightSettings;
	public Texture2D lightMap;
	public ComputeShader compute;


	ComputeBuffer allLights;

	public int numInstances = 100000;

	public int meshRes;
	public Shader instanceShader;

	public float sizeMin;
	public float sizeMax = 1;
	public float turnOnTimeVariation;
	public float turnOnTime;
	public Color colourDim;
	public Color colourBright;

	public int[] precalculatedBufferSizes;

	[Header("Debug")]
	[SerializeField] Mesh mesh;
	public bool calculateBufferSize;

	CityLightRenderer[] renderers;
	Camera cam;
	Transform sunLight;
	bool initialized;


	public void Init(RenderTexture heightMap, Bounds[] allBounds, Light sunLight)
	{
		if (enableCityLights)
		{
			IcoSphere sphere = new IcoSphere(meshRes);
			mesh = sphere.GetMesh(radius: 0.5f);
			this.sunLight = sunLight.transform;
			cam = Camera.main;
			ComputeHelper.CreateStructuredBuffer<CityLight>(ref allLights, numInstances);

			// Set positions in compute shader
			compute.SetTexture(0, "LightMap", lightMap);
			compute.SetTexture(0, "HeightMap", heightMap);
			compute.SetBuffer(0, "CityLights", allLights);
			compute.SetInt("numLights", numInstances);
			compute.SetFloat("worldRadius", heightSettings.worldRadius);
			compute.SetFloat("heightMultiplier", heightSettings.heightMultiplier);
			ComputeHelper.Dispatch(compute, numInstances);

			// Partition
			compute.SetBuffer(1, "CityLights", allLights);
			renderers = new CityLightRenderer[allBounds.Length];

			if (precalculatedBufferSizes.Length != allBounds.Length)
			{
				precalculatedBufferSizes = new int[allBounds.Length];
				Debug.Log("City light buffer sizes array is wrong size");
			}

			for (int i = 0; i < allBounds.Length; i++)
			{
				var cityLightRenderer = new CityLightRenderer(mesh, allBounds[i], instanceShader, precalculatedBufferSizes[i]);
				AssignConstantShaderData(cityLightRenderer);
				renderers[i] = cityLightRenderer;

				compute.SetBuffer(1, "PartitionedLights", cityLightRenderer.lightBuffer);
				compute.SetVector("boundsCentre", cityLightRenderer.bounds.center);
				compute.SetVector("boundsSize", cityLightRenderer.bounds.size);
				ComputeHelper.Dispatch(compute, numInstances, kernelIndex: 1);
				renderers[i].Init();

				// Very hacky... (todo: figure out better solution)
				// Run game once with calculateBufferSize set to true. This will populate precalculated with actual number of items in append buffers.
				// These values can then be saved for next time, to give the buffers the correct size and not waste any memory
				if (calculateBufferSize)
				{
					var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
					ComputeBuffer.CopyCount(cityLightRenderer.lightBuffer, countBuffer, 0);
					int[] result = new int[1];
					countBuffer.GetData(result);
					int numPointsInBuffer = result[0];
					countBuffer.Release();

					precalculatedBufferSizes[i] = numPointsInBuffer;
				}

			}

			ComputeHelper.Release(allLights);
		}
		initialized = true;
	}



	void Update()
	{
		if (!initialized)
		{
			return;
		}

		if (enableCityLights)
		{
			foreach (var r in renderers)
			{
				if (AnyLightOnInBounds(r.bounds))
				{
					Vector3 p = r.bounds.center + r.bounds.extents;
					UpdateShaderProperties(r);
					r.Render(cam);
				}
			}
		}
	}

	// Test if bounds falls inside region dark enough for city lights to display
	bool AnyLightOnInBounds(Bounds bounds)
	{
		Vector3 h = bounds.extents;
		// Check all corners
		for (int z = -1; z <= 1; z += 2)
		{
			for (int y = -1; y <= 1; y += 2)
			{
				for (int x = -1; x <= 1; x += 2)
				{
					if (LightAtPointWouldBeOn(bounds.center + new Vector3(h.x * x, h.y * y, h.z * z)))
					{
						return true;
					}
				}
			}
		}
		return false;

	}

	bool LightAtPointWouldBeOn(Vector3 point)
	{
		float threshold = turnOnTime + 0.5f * turnOnTimeVariation;
		Vector3 dir = point.normalized;
		return Vector3.Dot(dir, -sunLight.forward) < threshold;
	}

	void UpdateShaderProperties(CityLightRenderer r)
	{
		r.material.SetVector("dirToSun", -sunLight.forward);
		// These should be constant at runtime, but update in editor for tweaking / recompiling
		if (Application.isEditor)
		{
			AssignConstantShaderData(r);
		}
	}

	void AssignConstantShaderData(CityLightRenderer r)
	{
		r.material.SetColor("colourDim", colourDim);
		r.material.SetColor("colourBright", colourBright);
		r.material.SetFloat("sizeMin", sizeMin);
		r.material.SetFloat("sizeMax", sizeMax);
		r.material.SetFloat("turnOnTimeVariation", turnOnTimeVariation);
		r.material.SetFloat("turnOnTime", turnOnTime);
		r.material.SetBuffer("CityLights", r.lightBuffer);
	}

	void OnDestroy()
	{
		if (renderers != null)
		{
			foreach (var r in renderers)
			{
				r.Release();
			}
		}
	}

	struct CityLight
	{
		Vector3 pointOnSphere;
		float height;
		float intensity;
		float randomT;
		int inRenderGroup;
	}

	public class CityLightRenderer
	{
		public ComputeBuffer lightBuffer;
		public ComputeBuffer renderArgs;
		public Bounds bounds;
		public Material material;
		Mesh mesh;

		public CityLightRenderer(Mesh mesh, Bounds bounds, Shader shader, int numInstances)
		{
			this.mesh = mesh;
			this.bounds = bounds;
			material = new Material(shader);

			lightBuffer = ComputeHelper.CreateAppendBuffer<CityLight>(Mathf.Max(1, numInstances));

		}

		public void Init()
		{
			renderArgs = ComputeHelper.CreateArgsBuffer(mesh, lightBuffer);
		}


		public void Release()
		{
			ComputeHelper.Release(lightBuffer, renderArgs);
		}

		public void Render(Camera cam)
		{
			Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, renderArgs, camera: cam, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
		}

	}
}
