using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Localized Data")]
public class LocalizedData : ScriptableObject
{
	public LocalizeManager.Language language;
	public TextAsset[] localizedStringGroups;
	public LocalizedTextChunk[] localizedTextChunks;

	public LocalizedString[] Load()
	{
		List<LocalizedString> localizedStrings = new List<LocalizedString>();

		if (localizedStringGroups != null)
		{
			foreach (var file in localizedStringGroups)
			{
				LocalizedStringGroup group = JsonUtility.FromJson<LocalizedStringGroup>(file.text);
				localizedStrings.AddRange(group.entries);
			}
		}

		if (localizedTextChunks != null)
		{
			foreach (var chunk in localizedTextChunks)
			{
				localizedStrings.Add(new LocalizedString(chunk.id, chunk.file.text));
			}
		}

		return localizedStrings.ToArray();
	}

#if UNITY_EDITOR
	public void UpdateFromRoot(LocalizedData root)
	{
		for (int i = 0; i < root.localizedStringGroups.Length; i++)
		{
			LocalizedString[] rootLocalizedStrings = JsonUtility.FromJson<LocalizedStringGroup>(root.localizedStringGroups[i].text).entries;
			LocalizedString[] myLocalizedStrings = JsonUtility.FromJson<LocalizedStringGroup>(localizedStringGroups[i].text).entries;
			LocalizedString[] updatedLocalizedStrings = UpdateFromRoot(rootLocalizedStrings, myLocalizedStrings);
			string updateJson = JsonUtility.ToJson(new LocalizedStringGroup(updatedLocalizedStrings), prettyPrint: true);
			//localizedStringGroups[i].na
			string path = UnityEditor.AssetDatabase.GetAssetPath(localizedStringGroups[i]);

			FileHelper.SaveTextToFile(path, updateJson, true);
		}
	}

	LocalizedString[] UpdateFromRoot(LocalizedString[] rootLocalizedStrings, LocalizedString[] myLocalizedStrings)
	{
		Queue<LocalizedString> root = new Queue<LocalizedString>(rootLocalizedStrings);
		Queue<LocalizedString> mine = new Queue<LocalizedString>(myLocalizedStrings);
		List<LocalizedString> updated = new List<LocalizedString>();

		while (root.Count > 0)
		{
			var element = root.Dequeue();
			// Element matches root
			if (mine.Count > 0 && mine.Peek().id == element.id)
			{
				updated.Add(mine.Dequeue());
			}
			else
			{
				// Insert new element from root
				updated.Add(element);
			}
		}

		return updated.ToArray();
	}

#endif

	[System.Serializable]
	public struct LocalizedTextChunk
	{
		public string id;
		public TextAsset file;
	}
}
