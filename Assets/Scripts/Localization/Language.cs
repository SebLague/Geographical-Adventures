using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GeoGame.Localization
{
	[CreateAssetMenu(menuName = "Localized Data")]
	public class Language : ScriptableObject
	{
		public string languageDisplayName;
		public string languageID;
		public bool rightToLeftWritingSystem;
		[Header("Data")]
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


		[System.Serializable]
		public struct LocalizedTextChunk
		{
			public string id;
			public TextAsset file;
		}
	}
}