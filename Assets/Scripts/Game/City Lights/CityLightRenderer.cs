using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityLightRenderer : MonoBehaviour
{

	public int meshRes = 1;
	public Shader shader;
	Material cityLightMat;
	Mesh mesh;

	ComputeBuffer buffer;
	ComputeBuffer args;

	void Start()
	{
		mesh = Seb.Meshing.IcoSphere.Generate(meshRes, 0.5f).ToMesh();

		buffer = GetComponent<CityLightGenerator>().allLights;
		args = ComputeHelper.CreateArgsBuffer(mesh, buffer.count);
		cityLightMat = new Material(shader);
	}



	void Update()
	{
		Graphics.DrawMeshInstancedIndirect(mesh, 0, cityLightMat, new Bounds(Vector3.zero, Vector3.one * 1000), args, camera: null, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
	}

	void OnDestroy()
	{
		ComputeHelper.Release(args);
	}
}
