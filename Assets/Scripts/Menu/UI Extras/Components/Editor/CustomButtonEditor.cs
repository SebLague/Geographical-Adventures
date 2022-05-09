using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomButton))]
public class CustomButtonEditor : UnityEditor.UI.ButtonEditor
{
	CustomButton customButton;

	public override void OnInspectorGUI()
	{
		// Draw custom button variables
		// (but ignore inherited variables as those are handled by Unity's custom editor)
		SerializedProperty property = serializedObject.GetIterator();
		bool enterChildren = true;
		while (property.NextVisible(enterChildren))
		{
			if (typeof(CustomButton).GetField(property.name) != null)
			{
				enterChildren = DrawProperty(property);
			}
			else
			{
				enterChildren = false;
			}
		}
		serializedObject.ApplyModifiedProperties();

		GUILayout.Space(15);
		EditorGUILayout.LabelField("Base Button Settings", EditorStyles.boldLabel);

		// Draw inherted button inspector
		base.OnInspectorGUI();
	}

	bool DrawProperty(SerializedProperty property)
	{
		bool drawProperty = true;
		if (drawProperty)
		{
			EditorGUILayout.PropertyField(property);
		}
		return drawProperty;
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		customButton = target as CustomButton;
		if (customButton.label == null)
		{
			customButton.label = customButton.gameObject.GetComponentInChildren<TMPro.TMP_Text>();
		}
	}

}
