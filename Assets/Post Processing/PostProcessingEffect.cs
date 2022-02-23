using UnityEngine;

// Base class for various post processing effects.
// Functions are called by the PostProcessingManager
public abstract class PostProcessingEffect : ScriptableObject
{
	public bool enabled = true;
	public Shader shader;
	protected Material material;
	RenderTexture target;

	public virtual void OnEnable()
	{

	}

	public virtual RenderTexture Render(RenderTexture source)
	{
		CreateMaterial(ref material, shader);

		target = RenderTexture.GetTemporary(source.descriptor);
		RenderEffectToTarget(source, target);
		return target;
	}

	public virtual void OnFinishedDrawingFrame()
	{
		if (target)
		{
			RenderTexture.ReleaseTemporary(target);
		}
	}

	public virtual void OnDestroy()
	{

	}

	public virtual void DrawGizmos()
	{

	}

	protected abstract void RenderEffectToTarget(RenderTexture source, RenderTexture target);

	protected void CreateMaterial(ref Material mat, Shader shader)
	{
		if (mat == null || mat.shader != shader)
		{
			if (shader == null)
			{
				Debug.LogError("Shader is null, falling back to Unlit/Texture.");
				shader = Shader.Find("Unlit/Texture");
			}
			mat = new Material(shader);
			mat.hideFlags = HideFlags.HideAndDontSave;
		}

	}

}