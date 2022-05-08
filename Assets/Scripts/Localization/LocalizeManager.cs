using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizeManager : MonoBehaviour
{
	public enum Language { English, French, German }
	public static event System.Action onLanguageChanged;

	public LocalizedData[] languagePacks;
	[NaughtyAttributes.ReadOnly] public Language activeLanguage;

	public LocalizedData rootLanguage;

	static LocalizeManager instance;

	Dictionary<Language, Dictionary<string, string>> languageLookup;

	void Awake()
	{
		instance = this;
		languageLookup = new Dictionary<Language, Dictionary<string, string>>();

		foreach (LocalizedData languagePack in languagePacks)
		{
			LocalizedString[] localizedStrings = languagePack.Load();
			foreach (var entry in localizedStrings)
			{
				Add(languagePack.language, entry);
			}
		}

	}

	void Add(Language language, LocalizedString entry)
	{

		if (!string.IsNullOrWhiteSpace(entry.text))
		{
			if (!languageLookup.ContainsKey(language))
			{
				languageLookup.Add(language, new Dictionary<string, string>());
			}

			languageLookup[language].Add(entry.id, entry.text);
		}
	}

	public void ChangeLanguage(Language language)
	{
		if (language != activeLanguage)
		{
			activeLanguage = language;
			onLanguageChanged?.Invoke();
		}
	}

	public static string Localize(string id)
	{
		// Use english as fallback if no localization for active language
		var lookup = instance.languageLookup[Language.English];

		// Try get localized text
		if (instance.languageLookup.ContainsKey(instance.activeLanguage))
		{
			lookup = instance.languageLookup[instance.activeLanguage];
			if (lookup.ContainsKey(id))
			{
				return lookup[id];
			}
		}

		// No entry found for active language; falling back to english
		if (lookup.ContainsKey(id))
		{
			return lookup[id];
		}
		// No entry found
		string missing = $"Missing entry: {id}";
		Debug.LogError(missing);
		return missing;
	}

#if UNITY_EDITOR
	[NaughtyAttributes.Button]
	public void UpdateFromRoot()
	{
		foreach (var languagePack in languagePacks)
		{
			if (languagePack != rootLanguage)
			{
				languagePack.UpdateFromRoot(rootLanguage);
			}
		}
	}
#endif

	void OnDestroy()
	{
		onLanguageChanged = null;
	}

	void OnValidate()
	{

	}
}
