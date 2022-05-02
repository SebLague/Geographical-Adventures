using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class RenderingManager : MonoBehaviour
{

	public SolarSystem.StarRenderer starRenderer;
	public AtmosphereEffect atmosphereEffect;

	bool atmosphereActive;
	CommandBuffer outerSpaceRenderCommand;
	CommandBuffer skyRenderCommand;
	Camera cam;

	public Mesh mesh;
	public Material mat;
	public SolarSystem.Moon moon;

	void OnEnable()
	{
		Setup();
	}

	void Setup()
	{
		cam = Camera.main;
		cam.RemoveAllCommandBuffers();

		outerSpaceRenderCommand = new CommandBuffer();
		outerSpaceRenderCommand.name = "Outer Space Render";


		starRenderer?.SetUpStarRenderingCommand(outerSpaceRenderCommand);
		moon?.Setup(outerSpaceRenderCommand);
		cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, outerSpaceRenderCommand);

		// Atmosphere
		skyRenderCommand = new CommandBuffer();
		skyRenderCommand.name = "Sky Render";
		atmosphereEffect.SetupSkyRenderingCommand(skyRenderCommand);

		atmosphereActive = atmosphereEffect.enabled;
		if (atmosphereActive)
		{
			cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, skyRenderCommand);
		}
	}

	void Update()
	{
		//Graphics.DrawMesh(mesh, Matrix4x4.TRS(new Vector3(70, 134, -80), Quaternion.identity, Vector3.one * 30), mat, 0);
		if (atmosphereEffect.enabled != atmosphereActive)
		{
			atmosphereActive = atmosphereEffect.enabled;
			if (atmosphereActive)
			{
				cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, skyRenderCommand);
			}
			else
			{
				cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, skyRenderCommand);
			}
		}
	}

	void OnDisable()
	{
		skyRenderCommand?.Release();
		outerSpaceRenderCommand?.Release();
	}

}
