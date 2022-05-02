using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpFloodTest : MonoBehaviour
{

	public Texture2D mask;
	public MeshRenderer display;
	public ComputeShader compute;
	[Header("Debug")]
	public RenderTexture result;
	public Material testMat;


	void Awake()
	{
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
		ComputeHelper.CreateRenderTexture(ref result, mask.width, mask.height, FilterMode.Bilinear, format, useMipMaps: true);

		Graphics.Blit(mask, result);


		compute.SetTexture(0, "Texture", result);
		compute.SetTexture(1, "Texture", result);
		compute.SetInt("width", mask.width);
		compute.SetInt("height", mask.height);

		ComputeHelper.Dispatch(compute, result.width, result.height, kernelIndex: 0);

		Run();

		result.GenerateMips();

		if (display)
		{
			display.material.mainTexture = result;
		}
		if (testMat)
		{
			testMat.SetTexture("Dst", result);
		}
	}

	void Run()
	{
		int size = Mathf.Max(mask.width, mask.height);
		int jumpSize = size / 2;

		while (jumpSize > 0)
		{
			compute.SetInt("jumpSize", jumpSize);
			ComputeHelper.Dispatch(compute, result.width, result.height, kernelIndex: 1);

			jumpSize /= 2;
		}
	}


}
