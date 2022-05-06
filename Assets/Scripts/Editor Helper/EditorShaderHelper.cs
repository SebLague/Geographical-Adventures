// TODO: find a less horribly hacky way to handle this...

// The problem is that if I edit a shader during play mode, after recompiling it will lose references to any rendertextures/buffers
// that were set from script. I haven't yet been able to find a reliable way of detecting shader recompiles (*) so instead I'm
// just detecting when the Unity editor regains focus, and triggering the 'onRebindRequired' event then.

// (*) Have tried ShaderUtils.anythingCompiling, but not sure how to poll it frequently enough to catch small shaders that compile very quickly.
// 	Maybe look into AssetPostprocessor ?
#if UNITY_EDITOR
public static class EditorShaderHelper
{

	public static event System.Action onRebindRequired;
	static bool editorHasFocus;


	static EditorShaderHelper()
	{
		editorHasFocus = true;
		UnityEditor.EditorApplication.update += Update;
		UnityEditor.EditorApplication.playModeStateChanged += PlaymodeStateChanged;
	}

	static void PlaymodeStateChanged(UnityEditor.PlayModeStateChange state)
	{
		// If exiting play mode, then clear all listeners from the event
		if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
		{
			onRebindRequired = null;
		}
	}

	static void Update()
	{
		if (UnityEditor.EditorApplication.isPlaying)
		{
			// If unity editor regains focus then invoke the event
			bool focus = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
			if (focus && !editorHasFocus)
			{
				onRebindRequired?.Invoke();
			}
			editorHasFocus = focus;
		}
	}

}
#endif