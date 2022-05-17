using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Localization
{
	public class LocalizationManager : MonoBehaviour
	{
		public static event System.Action onLanguageChanged;

		public Language[] languages;
		public Language defaultLanguage;
		Language activeLanguage;

		Dictionary<Language, Dictionary<string, string>> languageLookup;
		static LocalizationManager _instance;

		void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				Init();
			}
			else if (_instance == this)
			{
				// Already initialized
			}
			else
			{
				// Duplicate
				Debug.Log("Destroying duplicate LocalizationManager: " + gameObject.name);
				Destroy(this);
			}

		}

		void Init()
		{
			activeLanguage = languages[0];
			languageLookup = new Dictionary<Language, Dictionary<string, string>>();

			foreach (Language language in languages)
			{
				LocalizedString[] localizedStrings = language.Load();
				foreach (var entry in localizedStrings)
				{
					Add(language, entry);
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

		public void ChangeLanguage(string languageID)
		{
			Language language = languages[GetIndexFromID(languageID)];
			ChangeLanguage(language);
		}

		public void ChangeLanguage(Language language)
		{
			if (language != activeLanguage)
			{
				activeLanguage = language;
				onLanguageChanged?.Invoke();
			}
		}

		public Language ActiveLanguage
		{
			get
			{
				return activeLanguage;
			}
		}


		public static string Localize(string id)
		{
			Dictionary<string, string> lookup;

			// Try get localized text
			if (Instance.languageLookup.ContainsKey(Instance.activeLanguage))
			{
				lookup = Instance.languageLookup[Instance.activeLanguage];
				if (lookup.ContainsKey(id))
				{
					return lookup[id];
				}
			}

			// No entry found for active language; falling back to default language
			lookup = Instance.languageLookup[Instance.defaultLanguage];

			if (lookup.ContainsKey(id))
			{
				return lookup[id];
			}
			// No entry found
			string missing = $"Missing entry: {id}";
			Debug.LogError(missing);
			return missing;
		}

		public static bool IsRightToLeftWritingSystem
		{
			get
			{
				return Instance.ActiveLanguage.rightToLeftWritingSystem;
			}
		}

		public int GetIndexFromID(string languageID)
		{
			int languageIndex = -1;
			int defaultLanguageIndex = 0;

			for (int i = 0; i < languages.Length; i++)
			{
				if (languages[i].languageID == languageID)
				{
					languageIndex = i;
				}
				if (languages[i].languageID == defaultLanguage.languageID)
				{
					defaultLanguageIndex = i;
				}
			}

			return (languageIndex >= 0) ? languageIndex : defaultLanguageIndex;
		}

		void OnDestroy()
		{
			onLanguageChanged = null;
		}

		void OnValidate()
		{

		}

		public static LocalizationManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<LocalizationManager>(includeInactive: true);
					_instance.Init();
				}
				return _instance;
			}
		}

	}
}