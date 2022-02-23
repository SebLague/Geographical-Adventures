using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class PostProcessingManager : MonoBehaviour
{

	public PostProcessingEffect[] effects;

	void OnEnable()
	{
		if (effects != null)
		{
			for (int i = 0; i < effects.Length; i++)
			{
				if (effects[i])
				{
					effects[i].OnEnable();
				}
			}
		}
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
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
