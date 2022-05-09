using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RebindUI))]
public class RebindUIEditor : Editor
{
	private static readonly string[] propertiesToExclude = new string[] { "m_Script" };

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		RebindUI rebindUI = target as RebindUI;
		if (rebindUI.inputActionReference != null)
		{
			var bindings = rebindUI.inputActionReference.action.bindings;
			string[] bindingNames = new string[bindings.Count];

			for (int i = 0; i < bindings.Count; i++)
			{
				string displayName = "";
				if (bindings[i].isComposite)
				{
					displayName = $"{bindings[i].name} (Composite Input):";
				}
				else
				{
					displayName = $"{bindings[i].ToDisplayString()} [{bindings[i].groups}";
				}

				bindingNames[i] = displayName;
			}
			rebindUI.bindingIndex = Mathf.Clamp(rebindUI.bindingIndex, 0, bindings.Count);
			int targetIndex = EditorGUILayout.Popup("Input", rebindUI.bindingIndex, bindingNames);
			// Using serializedObject (instead of rebindUI directly) to avoid changes getting reset by the shenanigans below
			serializedObject.FindProperty(nameof(rebindUI.bindingIndex)).intValue = targetIndex;
		}

		// Draw default inspector, but without the default script reference field (because it looks
		// strange if it comes after the custom editor stuff from above)
		DrawPropertiesExcluding(serializedObject, propertiesToExclude);
		serializedObject.ApplyModifiedProperties();

	}
}
