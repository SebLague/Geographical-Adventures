using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MaterialInspector))]
public class MaterialInspectorEditor : Editor
{

	Editor materialEditor;
	MaterialInspector materialInspector;

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		using (var check = new EditorGUI.ChangeCheckScope())
		{
			materialInspector = target as MaterialInspector;

			DrawSettingsEditor(materialInspector.material, ref materialEditor);

			if (check.changed)
			{
				materialInspector.NotifyMaterialUpdate();
			}
		}
	}


	void DrawSettingsEditor(Object settings, ref Editor editor)
	{
		if (settings != null)
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
