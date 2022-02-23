using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CloudsEffect))]
public class CloudEditor : Editor
{

	Material previewMat;
	CloudsEffect cloudEffect;
	Editor previewEditor;

	public override void OnInspectorGUI()
	{

		using (var check = new EditorGUI.ChangeCheckScope())
		{
			base.OnInspectorGUI();

			if (check.changed)
			{
				//cloudEffect.EditorUpdate();
			}
		}

		// Noise preview
		if (cloudEffect.showNoisePreview)
		{
			if (previewMat == null)
			{
				previewMat = new Material(Shader.Find("Hidden/NoisePreview"));
			}

			cloudEffect.ApplyPreviewMat(previewMat);

			Editor.CreateCachedEditor(previewMat, null, ref previewEditor);
			previewEditor.OnPreviewGUI(GUILayoutUtility.GetRect(500, 500), EditorStyles.whiteLabel);
		}
	}

	void OnEnable()
	{
		cloudEffect = target as CloudsEffect;
	}
}
