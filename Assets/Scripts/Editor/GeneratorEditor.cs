using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineInternal;

[CustomEditor(typeof(WorldMeshGenerator))]
public class GeneratorEditor : Editor
{

	bool showMaterialProperties;
	Editor materialEditor;


	public class Test : ScriptableObject
	{
		public List<int> myList;
	}
	Test test;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		WorldMeshGenerator gen = (target) as WorldMeshGenerator;//

		using (var check = new EditorGUI.ChangeCheckScope())
		{
			DrawSettingsEditor(gen.material, ref showMaterialProperties, ref materialEditor);
			if (check.changed)
			{
				if (Application.isPlaying)
				{
					gen.OnMaterialUpdated();
				}
			}
		}
	}


	void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
	{
		if (settings != null)
		{
			foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
			if (foldout)
			{
				CreateCachedEditor(settings, null, ref editor);
				MaterialEditor materialEditor = editor as MaterialEditor;
				if (materialEditor)
				{
					materialEditor.DrawHeader();
					materialEditor.OnInspectorGUI();
				}
			}
		}
	}

	void OnDisable()
	{
		EditorPrefs.SetBool(nameof(showMaterialProperties), showMaterialProperties);
	}

	private void OnEnable()
	{
		showMaterialProperties = EditorPrefs.GetBool(nameof(showMaterialProperties), false);
	}
}
