using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class NormalsGenerator : MonoBehaviour
{

	public TerrainGeneration.TerrainHeightSettings heightSettings;
	public Texture2D[] tilesWest;
	public Texture2D[] tilesEast;
	public Shader quaterTilesToHalfTile;

	public ComputeShader compute;

	public MeshRenderer display;

	RenderTexture normalsWest;
	RenderTexture normalsEast;
	RenderTexture heightWest;
	RenderTexture heightEast;

	const int size = 16384;

	void Start()
	{
		var normalsResultFormat = GraphicsFormat.R16G16B16A16_SFloat;
		ComputeHelper.CreateRenderTexture(ref normalsWest, size, size, FilterMode.Bilinear, normalsResultFormat);
		ComputeHelper.CreateRenderTexture(ref normalsEast, size, size, FilterMode.Bilinear, normalsResultFormat);

		var heightFormat = GraphicsFormat.R16_SFloat;
		ComputeHelper.CreateRenderTexture(ref heightWest, size, size, FilterMode.Bilinear, heightFormat);
		ComputeHelper.CreateRenderTexture(ref heightEast, size, size, FilterMode.Bilinear, heightFormat);

		// Combine 4 west tiles into one big west tile to make life easier
		Material stitchMat = new Material(quaterTilesToHalfTile);
		SetStitchMatTextures(stitchMat, tilesWest);
		Graphics.Blit(null, heightWest, stitchMat);
		// Do same thing for east tile
		SetStitchMatTextures(stitchMat, tilesEast);
		Graphics.Blit(null, heightEast, stitchMat);


		compute.SetTexture(0, "HeightWest", heightWest);
		compute.SetTexture(0, "HeightEast", heightEast);

		compute.SetTexture(0, "Normals", normalsWest);
		compute.SetInt("offset", 0);
		ComputeHelper.Dispatch(compute, size, size);

		compute.SetTexture(0, "Normals", normalsEast);
		compute.SetInt("offset", size);
		compute.SetFloat("worldRadius", heightSettings.worldRadius);
		compute.SetFloat("heightMultiplier", heightSettings.heightMultiplier);

		ComputeHelper.Dispatch(compute, size, size);
	}

	void SetStitchMatTextures(Material material, Texture2D[] tiles)
	{
		material.SetTexture("_TileA", tiles[0]);
		material.SetTexture("_TileB", tiles[1]);
		material.SetTexture("_TileC", tiles[2]);
		material.SetTexture("_TileD", tiles[3]);
	}

	// Update is called once per frame
	void Update()
	{
		display.material.SetTexture("NormalsWest", normalsWest);
		display.material.SetTexture("NormalsEast", normalsEast);
		//transform.position = transform.position.normalized;
	}

	void OnDrawGizmos()
	{
		//Gizmos.DrawSphere(CoordinateSystem.PointToCoordinate(transform.position).ToVector2(), 0.05f);
	}

	void OnDestroy()
	{
		ComputeHelper.Release(normalsWest, normalsEast, heightWest, heightEast);
	}
}
