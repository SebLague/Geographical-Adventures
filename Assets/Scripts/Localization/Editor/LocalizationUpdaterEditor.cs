using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GeoGame.Localization.CustomEditors
{
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
					case LocalizationUpdater.UpdateType.AddFirst:
						updater.newID = EditorGUILayout.TextField("New ID", updater.newID);
						updater.newValue = EditorGUILayout.TextField("Text Value to Add", updater.newValue);
						buttonName = "Add New Entry";
						break;
					case LocalizationUpdater.UpdateType.AddAfter:
						updater.addAfterID = EditorGUILayout.TextField("Add After", updater.addAfterID);
						updater.newID = EditorGUILayout.TextField("New ID", updater.newID);
						updater.newValue = EditorGUILayout.TextField("Text Value to Add", updater.newValue);
						using (new EditorGUI.DisabledGroupScope(updater.addAfterID == updater.newID))
						{
							if (GUILayout.Button("Set 'Add After' to 'New ID'"))
							{
								updater.addAfterID = updater.newID;
							}
						}
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
}