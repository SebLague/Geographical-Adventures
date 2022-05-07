using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PostProcessingManager : MonoBehaviour
{

	public PostProcessingEffect[] effects;
	Camera cam;

	void OnEnable()
	{
		cam = GetComponent<Camera>();
		if (effects != null)
		{
			for (int i = 0; i < effects.Length; i++)
			{
				if (effects[i])
				{
					effects[i].OnEnable();
					effects[i].SetCamera(cam);
				}
			}
		}
		cam.depthTextureMode = DepthTextureMode.Depth;
	}


	void OnRenderImage(RenderTexture source, RenderTexture target)
	{
		RenderTexture lastRenderedImage = source;

		if (effects == null)
		{
			effects = new PostProcessingEffect[0];
		}

		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i] && effects[i].enabled)
			{
				lastRenderedImage = effects[i].Render(lastRenderedImage);
			}
		}


		Graphics.Blit(lastRenderedImage, target);

		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i])
			{
				effects[i].OnFinishedDrawingFrame();
			}
		}
	}

	void OnDrawGizmos()
	{
		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i])
			{
				effects[i].DrawGizmos();
			}
		}
	}

	void OnDestroy()
	{
		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i])
			{
				effects[i].OnDestroy();
			}
		}
	}


}
