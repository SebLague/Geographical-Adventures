using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LocalizationUpdater))]
public class LocalizationUpdaterEditor : Editor
{
	public override void OnInspectorGUI()
	{
		LocalizationUpdater updater = target as LocalizationUpdater;

		updater.locked = EditorGUILayout.Toggle("Locked", updater.locked);

		using (new EditorGUI.DisabledGroupScope(updater.locked))
		{
			updater.updateType = (LocalizationUpdater.UpdateType)EditorGUILayout.EnumPopup("Update Type", updater.updateType);
			string buttonName = "";
			switch (updater.updateType)
			{
				case LocalizationUpdater.UpdateType.AddAfter:
					updater.addAfterID = EditorGUILayout.TextField("Previous ID", updater.addAfterID);
					updater.newID = EditorGUILayout.TextField("ID to Add", updater.newID);
					updater.newValue = EditorGUILayout.TextField("Text Value to Add", updater.newValue);
					buttonName = "Add New Entry";
					break;
				case LocalizationUpdater.UpdateType.Rename:
					updater.renameID = EditorGUILayout.TextField("ID to Rename", updater.renameID);
					updater.newID = EditorGUILayout.TextField("New ID", updater.newID);
					buttonName = "Rename Entry";
					break;
				case LocalizationUpdater.UpdateType.Remove:
					updater.removeID = EditorGUILayout.TextField("ID to Remove", updater.removeID);
					buttonName = "Remove Entry";
					break;

			}

			if (GUILayout.Button(buttonName))
			{
				updater.Run();
			}
		}
	}
}
