using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class PostProcessingManager : MonoBehaviour
{

	public PostProcessingEffect[] effects;
	public Material mat;

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

		if (Application.isPlaying)
		{
			UnityEngine.Rendering.CommandBuffer cmd = new UnityEngine.Rendering.CommandBuffer();
			int id = Shader.PropertyToID("_AtmoTestRT");
			cmd.GetTemporaryRT(id, -1, -1, 24, FilterMode.Bilinear);
			cmd.SetRenderTarget(id);
			cmd.Blit(BuiltinRenderTextureType.CurrentActive, id, effects[0].GetMaterial);
			//cmd.Blit(id, BuiltinRenderTextureType.CameraTarget);
			//cmd.Blit(null, BuiltinRenderTextureType.CurrentActive, mat);
			//cmd.()
			//cmd.DispatchCompute()
			//Camera.main.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, cmd);
			//Camera.main.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterImageEffects, cmd);
			//Camera.onPreRender += PreRender;
		}
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
