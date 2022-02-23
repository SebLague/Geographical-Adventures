using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChannelPack : MonoBehaviour
{

	[Header("Data")]
	public Texture2D[] colourSource;
	public Texture2D[] alphaSource;

	[Header("Output")]
	public string savePath;
	public string saveFileName;

	[Header("References")]
	public ComputeShader compute;

	[Header("Debug")]
	public RenderTexture result;

	void Start()
	{
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
		result = ComputeHelper.CreateRenderTexture(colourSource[0].width, colourSource[0].height, FilterMode.Bilinear, format);
		compute.SetTexture(0, "Result", result);
		compute.SetInts("size", result.width, result.height);

		StartCoroutine(Run());
	}

	IEnumerator Run()
	{
		Debug.Log($"Processing {colourSource.Length} image/s.");
		yield return null;

		for (int i = 0; i < colourSource.Length; i++)
		{
			Process(i);
			Debug.Log($"Image {i + 1} of {colourSource.Length} completed");
			yield return null;
		}

		Debug.Log("Finished");
	}

	void Process(int i)
	{
		compute.SetTexture(0, "ColourSource", colourSource[i]);
		compute.SetTexture(0, "AlphaSource", alphaSource[i]);

		ComputeHelper.Dispatch(compute, result);

		WriteToPng(result, savePath, saveFileName + "_" + i);
	}

	public static void WriteToPng(RenderTexture renderTexture, string savePath, string fileName)
	{

		RenderTexture prevActiveRT = RenderTexture.active;

		Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
		RenderTexture.active = renderTexture;
		texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		texture.Apply();
		byte[] imageBytes = texture.EncodeToPNG();
		System.IO.File.WriteAllBytes(System.IO.Path.Combine(savePath, fileName + ".png"), imageBytes);
		RenderTexture.active = prevActiveRT;
	}

}
