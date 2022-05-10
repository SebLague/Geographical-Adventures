using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Localization
{
	public class LocalizationUpdater : MonoBehaviour
	{

		public enum UpdateType { AddFirst, AddAfter, Rename, Remove }

		public bool locked;
		public UpdateType updateType;
		public string addAfterID;
		public string removeID;
		public string renameID;

		public string newID;
		public string newValue;

#if UNITY_EDITOR

		public void Run()
		{
			LocalizationManager manager = FindObjectOfType<LocalizationManager>();

			bool allSuccessful = true;

			foreach (var languagePack in manager.languages)
			{
				bool anyFileInLanguageUpdated = false;
				foreach (var groupFile in languagePack.localizedStringGroups)
				{
					var localizedStrings = new List<LocalizedString>(JsonUtility.FromJson<LocalizedStringGroup>(groupFile.text).entries);
					bool updated = UpdateLocalization(localizedStrings);
					if (updated)
					{
						anyFileInLanguageUpdated = true;
						string updatedSaveString = JsonUtility.ToJson(new LocalizedStringGroup(localizedStrings.ToArray()), prettyPrint: true);
						string path = UnityEditor.AssetDatabase.GetAssetPath(groupFile);
						FileHelper.SaveTextToFile(path, updatedSaveString, log: false);
						break; // id should be unique, so we can exit here
					}
				}

				if (!anyFileInLanguageUpdated)
				{
					allSuccessful = false;
					Debug.Log("Failed to update language: " + languagePack.languageDisplayName);
				}
			}

			if (allSuccessful)
			{
				Debug.Log("All languages updated successfully");
			}

			UnityEditor.AssetDatabase.Refresh();
		}

		// Returns true if any update was made
		bool UpdateLocalization(List<LocalizedString> localizedStrings)
		{
			switch (updateType)
			{
				case UpdateType.AddFirst:
					return AddFirst(localizedStrings, newID, newValue);
				case UpdateType.AddAfter:
					return AddAfter(localizedStrings, addAfterID, newID, newValue);
				case UpdateType.Rename:
					return Rename(localizedStrings, renameID, newID);
				case UpdateType.Remove:
					return Remove(localizedStrings, removeID);
			}

			return false;

		}

		static bool AddFirst(List<LocalizedString> localizedStrings, string newID, string newValue)
		{
			LocalizedString newEntry = new LocalizedString(newID, newValue);
			localizedStrings.Insert(0, newEntry);
			return true;
		}

		static bool AddAfter(List<LocalizedString> localizedStrings, string addAfterID, string newID, string newValue)
		{
			for (int i = 0; i < localizedStrings.Count; i++)
			{
				if (localizedStrings[i].id == addAfterID)
				{
					LocalizedString newEntry = new LocalizedString(newID, newValue);
					if (i == localizedStrings.Count - 1)
					{
						localizedStrings.Add(newEntry);
					}
					else
					{
						localizedStrings.Insert(i + 1, newEntry);
					}
					return true;
				}
			}

			return false;
		}

		static bool Rename(List<LocalizedString> localizedStrings, string idToRename, string newID)
		{
			for (int i = 0; i < localizedStrings.Count; i++)
			{
				if (localizedStrings[i].id == idToRename)
				{
					var renamedEntry = new LocalizedString(newID, localizedStrings[i].text);
					localizedStrings[i] = renamedEntry;
					return true;
				}
			}

			return false;
		}


		static bool Remove(List<LocalizedString> localizedStrings, string idToRemove)
		{
			for (int i = 0; i < localizedStrings.Count; i++)
			{
				if (localizedStrings[i].id == idToRemove)
				{
					localizedStrings.RemoveAt(i);
					return true;
				}
			}

			return false;
		}
#endif
	}
}