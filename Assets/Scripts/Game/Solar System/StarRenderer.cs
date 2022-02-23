using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class StarRenderer : MonoBehaviour
{
	public SolarSystemManager solarSystemManager;
	public StarLoader loader;
	public AtmosphereEffect atmosphereEffect;
	public Shader starInstanceShader;
	public Light sun;
	//public Vector3 testParams;
	public float size;
	Material starMaterial;
	StarLoader.Star[] stars;

	Mesh quadMesh;
	ComputeBuffer argsBuffer;
	ComputeBuffer starDataBuffer;
	Camera cam;
	Bounds bounds;

	public float brightnessMultiplier;
	public float appearTimeMin;
	public float appearTimeMax;

	void Start()
	{
		cam = Camera.main;
		stars = loader.LoadStars();

		CreateQuadMesh();

		starMaterial = new Material(starInstanceShader);

		argsBuffer = ComputeHelper.CreateArgsBuffer(quadMesh, stars.Length);

		starDataBuffer = ComputeHelper.CreateStructuredBuffer(stars);
		starMaterial.SetBuffer("StarData", starDataBuffer);
		bounds = new Bounds(Vector3.zero, Vector3.one * 10);

		atmosphereEffect.onSettingsUpdated += SetSkyTexture;
		SetSkyTexture();
	}

	void SetSkyTexture()
	{
		if (atmosphereEffect.sky != null)
		{
			starMaterial.SetTexture("Sky", atmosphereEffect.sky);
		}
	}


	public void UpdateFixedStars(EarthOrbit earth, bool geocentric)
	{
		if (Application.isPlaying)
		{
			starMaterial.SetFloat("size", size);
			starMaterial.SetVector("centre", cam.transform.position);
			starMaterial.SetFloat("brightnessMultiplier", brightnessMultiplier);
			Matrix4x4 rotMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
			// Earth remains stationary and without rotation, so rotate the stars instead
			if (geocentric)
			{
				Matrix4x4.Rotate(Quaternion.Inverse(earth.earthRot));
			}

			starMaterial.SetMatrix("rotationMatrix", rotMatrix);


			bounds.center = cam.transform.position;
			Graphics.DrawMeshInstancedIndirect(quadMesh, 0, starMaterial, bounds, argsBuffer, castShadows: ShadowCastingMode.Off, receiveShadows: false);
			//Graphics.DrawMeshInstanced(quadMesh, 0, starInstanceShader,)//
		}
	}


	void CreateQuadMesh()
	{
		quadMesh = new Mesh();

		Vector3[] vertices = {
			new Vector3(-1,-1), // bottom left
			new Vector3(1,-1), // bottom right
			new Vector3(1,1), // top left
			new Vector3(-1, 1) // top right
		};

		int[] triangles = { 0, 2, 1, 0, 3, 2 };

		quadMesh.SetVertices(vertices);
		quadMesh.SetTriangles(triangles, 0, true);
	}

	void OnDestroy()
	{
		ComputeHelper.Release(argsBuffer, starDataBuffer);
		atmosphereEffect.onSettingsUpdated -= SetSkyTexture;
	}
}
