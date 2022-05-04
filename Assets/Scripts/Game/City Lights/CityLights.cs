using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

public class CityLights : MonoBehaviour
{
	public bool drawLights = true;
	public TextAsset cityLightsFile;

	public int meshRes;
	public Shader instanceShader;

	public Color colourDim;
	public Color colourBright;
	public float brightnessMultiplier = 1;
	public float sizeMin;
	public float sizeMax = 1;
	public float turnOnTimeVariation;
	public float turnOnTime;

	[Header("Debug")]
	[SerializeField, Disabled] Mesh mesh;

	CityLightRenderer[] renderers;

	Transform sunLight;

	CityLightGroup groups;
	ComputeBuffer cityLightBuffer;


	public void Init(RenderTexture heightMap, Light sunLight)
	{
		mesh = IcoSphere.Generate(meshRes, 0.5f).ToMesh();
		this.sunLight = sunLight.transform;

		CityLightGroup[] groups = CityLightGenerator.LoadFromFile(cityLightsFile);
		List<CityLight> lightsList = new List<CityLight>();

		for (int i = 0; i < groups.Length; i++)
		{
			lightsList.AddRange(groups[i].cityLights);
		}

		cityLightBuffer = ComputeHelper.CreateStructuredBuffer(lightsList);
		renderers = new CityLightRenderer[groups.Length];

		int lightCountCumul = 0;
		for (int i = 0; i < groups.Length; i++)
		{
			int bufferOffset = lightCountCumul;
			int numInstancesInGroup = groups[i].cityLights.Length;
			lightCountCumul += numInstancesInGroup;

			renderers[i] = new CityLightRenderer(bufferOffset, numInstancesInGroup, groups[i].bounds, mesh, instanceShader);
			UpdateDynamicShaderProperties(renderers[i]);
			AssignConstantShaderData(renderers[i]);
		}
	}



	void Update()
	{
		if (drawLights)
		{
			Vector3 dirToLight = -sunLight.forward;
			for (int i = 0; i < renderers.Length; i++)
			{
				if (renderers[i].ShouldRender(dirToLight))
				{
					UpdateDynamicShaderProperties(renderers[i]);
					renderers[i].Render();
				}
			}
		}
	}




	void UpdateDynamicShaderProperties(CityLightRenderer r)
	{
		r.material.SetVector(ShaderPropertyNames.dirToSunID, -sunLight.forward);
		// These should be constant at runtime, but update in editor for easy tweaking / recompiling
		if (Application.isEditor)
		{
			AssignConstantShaderData(r);
		}
	}

	void AssignConstantShaderData(CityLightRenderer r)
	{
		// Buffer
		r.material.SetBuffer("CityLights", cityLightBuffer);
		r.material.SetInt("bufferOffset", r.offset);
		// Settings
		r.material.SetColor("colourDim", colourDim);
		r.material.SetColor("colourBright", colourBright);
		r.material.SetFloat("brightnessMultiplier", brightnessMultiplier);
		r.material.SetFloat("sizeMin", sizeMin);
		r.material.SetFloat("sizeMax", sizeMax);
		r.material.SetFloat("turnOnTimeVariation", turnOnTimeVariation);
		r.material.SetFloat("turnOnTime", turnOnTime);
	}

	static class ShaderPropertyNames
	{
		public static int dirToSunID = Shader.PropertyToID("dirToSun");
	}

	void OnDestroy()
	{
		ComputeHelper.Release(cityLightBuffer);

		if (renderers != null)
		{
			foreach (var r in renderers)
			{
				r.Release();
			}
		}
	}


	public class CityLightRenderer
	{
		public ComputeBuffer renderArgs;
		public Bounds bounds;
		public Material material;
		public readonly int offset;
		Mesh mesh;
		int numInstances;

		public CityLightRenderer(int offset, int numInstances, Bounds bounds, Mesh mesh, Shader shader)
		{
			this.mesh = mesh;
			this.offset = offset;
			this.numInstances = numInstances;
			this.bounds = bounds;

			material = new Material(shader);
			renderArgs = ComputeHelper.CreateArgsBuffer(mesh, numInstances);
		}


		public void Release()
		{
			ComputeHelper.Release(renderArgs);
			Destroy(material);
		}

		public void Render()
		{
			var shadowMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, renderArgs, camera: null, castShadows: shadowMode, receiveShadows: false);
		}

		// TODO: test/improve this
		public bool ShouldRender(Vector3 dirToSun)
		{
			var p = bounds.ClosestPoint(bounds.center - dirToSun * 1000);
			return Vector3.Dot(dirToSun, p.normalized) < 0.2f;
		}


	}
}
