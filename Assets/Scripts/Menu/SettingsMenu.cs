using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using GeoGame.Localization;

public class SettingsMenu : Menu
{
	public enum SettingsTab { Graphics, Audio, Controls }
	public SettingsTab defaultTab;

	[Header("Graphics Settings")]
	public Vector2Int[] supportedRatios;
	public ValueWheel aspectRatioWheel;
	public ValueWheel resolutionWheel;
	public Toggle fullscreenToggle;
	public Toggle vsyncToggle;
	public ValueWheel terrainQuality;
	public ValueWheel shadowQuality;

	[Header("Audio/Language Settings")]
	public ValueWheel languageWheel;
	public Slider masterVolumeSlider;
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;
	[Space()]
	public AudioMixer audioMixer;
	public LocalizationManager localizationManager;


	[Header("Other References")]
	public TabGroup tabGroup;
	public Button applyButton;

	// Private stuff
	Dictionary<Vector2Int, List<Vector2Int>> supportedResolutions;
	Settings lastAppliedSettings;


	protected override void Awake()
	{
		base.Awake();
	}

	void Start()
	{
		AddListeners();
		ApplySettings(Settings.LoadSavedSettings());
		SetUpScreen();
	}

	void AddListeners()
	{
		applyButton.onClick.AddListener(ApplyCurrentSettings);
		languageWheel.onValueChanged += OnLanguageChanged;
		masterVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());
		musicVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());
		sfxVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());

	}

	void OnLanguageChanged(int index)
	{
		localizationManager.ChangeLanguage(localizationManager.languages[index]);
	}

	// Set UI state from loaded settings
	void SetUIFromSettings(Settings settings)
	{
		// Graphics
		fullscreenToggle.SetIsOnWithoutNotify(settings.isFullscreen);
		vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);
		InitResolutionSettings(settings.screenSize);
		terrainQuality.SetActiveIndex((int)settings.terrainQuality, notify: false);
		shadowQuality.SetActiveIndex((int)settings.shadowQuality, notify: false);

		// Audio / Language
		InitLanguageUI();
		languageWheel.SetActiveIndex(localizationManager.GetIndexFromID(settings.languageID), notify: false);
		masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
		musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
		sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

	}

	// Construct settings struct from user's chosen settings
	Settings GetSettingsFromUI()
	{
		Settings settings = new Settings();
		// Graphics
		settings.isFullscreen = fullscreenToggle.isOn;
		if (!Application.isEditor)
		{
			settings.screenSize = GetCurrentResolutionOptions()[resolutionWheel.activeValueIndex];
		}
		settings.vsyncEnabled = vsyncToggle.isOn;
		settings.terrainQuality = (Settings.TerrainQuality)terrainQuality.activeValueIndex;
		settings.shadowQuality = (Settings.ShadowQuality)shadowQuality.activeValueIndex;

		// Audio / Language
		settings.languageID = localizationManager.languages[languageWheel.activeValueIndex].languageID;
		settings.masterVolume = masterVolumeSlider.value;
		settings.sfxVolume = sfxVolumeSlider.value;
		settings.musicVolume = musicVolumeSlider.value;
		return settings;
	}

	// Apply current settings to the game
	void ApplyCurrentSettings()
	{
		Settings currentSettings = GetSettingsFromUI();
		RebindManager.Instance.SaveAndApplyBindings();
		ApplySettings(currentSettings);
	}

	// Applies the settings and saves them to disc
	void ApplySettings(Settings settings)
	{
		// Apply audio / language settings
		localizationManager.ChangeLanguage(settings.languageID);
		UpdateAudioVolume(settings.masterVolume, settings.musicVolume, settings.sfxVolume);

		// Apply graphics settings
		FullScreenMode mode = (settings.isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		Screen.SetResolution(settings.screenSize.x, settings.screenSize.y, mode);
		QualitySettings.vSyncCount = (settings.vsyncEnabled) ? 1 : 0;

		RenderSettingsController.SetTerrainQuality(settings.terrainQuality);
		RenderSettingsController.SetShadowQuality(settings.shadowQuality);


		// Save
		lastAppliedSettings = settings;
		Settings.Save(settings);
	}


	void InitLanguageUI()
	{
		string[] languageDisplayNames = new string[localizationManager.languages.Length];
		for (int i = 0; i < languageDisplayNames.Length; i++)
		{
			languageDisplayNames[i] = localizationManager.languages[i].languageDisplayName;
		}
		languageWheel.SetPossibleValues(languageDisplayNames, 0);
	}

	void SetUpScreen()
	{
		const string initialScreenSizeDeterminedKey = "initialScreenSizeDetermined";
		bool isFirstScreenSetup = PlayerPrefs.GetInt(initialScreenSizeDeterminedKey, 0) == 0;

		// The game currently struggles with rendering at high resolutions.
		// As a (hopefully temporary) work-around, we'll restrict the game's resolution the first time it's launched.
		// The player can then change it in the settings if they want, and we won't intefere again.
		if (isFirstScreenSetup)
		{
			Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
			const int initialResolutionThresholdX = 2560;
			const int initialResolutionThresholdY = 1440;

			// Check if screen size has exceeded the startup threshold, and if so, try find a lower resolution to switch to
			if (screenSize.x * screenSize.y > initialResolutionThresholdX * initialResolutionThresholdY)
			{
				Vector2Int screenRatio = ResolutionSettingsHelper.GetRatio(Screen.currentResolution.width, Screen.currentResolution.height);
				int n = Mathf.Max(1, Mathf.CeilToInt(initialResolutionThresholdX / screenRatio.x));
				Vector2Int newInitialResolution = new Vector2Int(n * screenRatio.x, n * screenRatio.y);
				Screen.SetResolution(newInitialResolution.x, newInitialResolution.y, Screen.fullScreenMode);
				//Debug.Log("Setting from " + screenSize + "   to   " + newInitialResolution + "  aspect = " + screenRatio);
			}


			// Flag that initial screen size has been determined so this doesn't run on subsequent launches
			PlayerPrefs.SetInt(initialScreenSizeDeterminedKey, 1);
			PlayerPrefs.Save();
		}
	}

	void InitResolutionSettings(Vector2Int currentScreenSize)
	{
		// Create dictionary of supported resolutions for each supported aspect ratio
		supportedResolutions = new Dictionary<Vector2Int, List<Vector2Int>>();

		foreach (Vector2Int supportedRatio in supportedRatios)
		{
			supportedResolutions.Add(supportedRatio, new List<Vector2Int>());
		}

		Resolution[] monitorResolutions = Screen.resolutions;

		foreach (Resolution resolution in monitorResolutions)
		{
			Vector2Int size = new Vector2Int(resolution.width, resolution.height);
			Vector2Int aspectRatio;
			if (IsSupportedAspectRatio(size, out aspectRatio))
			{
				if (!supportedResolutions[aspectRatio].Contains(size))
				{
					supportedResolutions[aspectRatio].Add(size);
				}
			}
		}

		// Set up ratio display
		string[] supportedRatioStrings = new string[supportedRatios.Length];
		for (int i = 0; i < supportedRatios.Length; i++)
		{
			supportedRatioStrings[i] = $"{supportedRatios[i].x} : {supportedRatios[i].y}";
		}

		int currentAspectRatioIndex;
		if (!IsSupportedAspectRatio(currentScreenSize, out currentAspectRatioIndex))
		{
			// No exact match, so find closest ratio
			float ratio = currentScreenSize.x / (float)currentScreenSize.y;
			float bestMatch = float.MaxValue;
			for (int i = 0; i < supportedRatios.Length; i++)
			{
				float supportedRatio = supportedRatios[i].x / (float)supportedRatios[i].y;
				if (Mathf.Abs(ratio - supportedRatio) < bestMatch)
				{
					bestMatch = Mathf.Abs(ratio - supportedRatio);
					currentAspectRatioIndex = i;
				}
			}
		}

		aspectRatioWheel.SetPossibleValues(supportedRatioStrings, currentAspectRatioIndex);
		SetResolutionOptions(currentAspectRatioIndex);
		aspectRatioWheel.onValueChanged -= SetResolutionOptions;
		aspectRatioWheel.onValueChanged += SetResolutionOptions;

		// Init fullscreen toggle
		fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
	}

	Vector2Int[] GetCurrentResolutionOptions()
	{
		Vector2Int aspectRatio = supportedRatios[aspectRatioWheel.activeValueIndex];
		return supportedResolutions[aspectRatio].ToArray();
	}

	// Called when the selected aspect ratio changes
	void SetResolutionOptions(int aspectRatioIndex)
	{
		// Create resolution names
		Vector2Int[] resolutions = GetCurrentResolutionOptions();
		string[] resolutionNames = new string[resolutions.Length];
		for (int i = 0; i < resolutions.Length; i++)
		{
			resolutionNames[i] = $"{resolutions[i].x} x {resolutions[i].y}";
		}

		// Find resolution that most closely matches current resolution to display by default
		Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
		int resolutionIndex = 0;
		int closestResolutionMatch = int.MaxValue;

		for (int i = 0; i < resolutions.Length; i++)
		{
			int dst = (currentScreenSize - resolutions[i]).sqrMagnitude;
			if (dst < closestResolutionMatch)
			{
				closestResolutionMatch = dst;
				resolutionIndex = i;
			}
		}
		resolutionWheel.SetPossibleValues(resolutionNames, resolutionIndex);
	}


	void OnFullscreenChanged(bool isFullscreen)
	{
		FullScreenMode mode = (isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		Screen.SetResolution(Screen.width, Screen.height, mode);
	}

	public bool IsSupportedAspectRatio(Vector2Int res, out Vector2Int ratio)
	{
		int index;
		if (IsSupportedAspectRatio(res, out index))
		{
			ratio = supportedRatios[index];
			return true;
		}

		ratio = Vector2Int.zero;
		return false;
	}

	public bool IsSupportedAspectRatio(Vector2Int res, out int ratioIndex)
	{
		for (int i = 0; i < supportedRatios.Length; i++)
		{
			Vector2Int supportedRatio = supportedRatios[i];
			if (res.x % supportedRatio.x == 0 && res.y % supportedRatio.y == 0)
			{
				if (res.x / supportedRatio.x == res.y / supportedRatio.y)
				{
					ratioIndex = i;
					return true;
				}
			}
		}
		ratioIndex = -1;
		return false;
	}


	protected override void OnMenuOpened()
	{
		if (Application.isPlaying)
		{
			lastAppliedSettings = Settings.LoadSavedSettings();
			RebindManager.Instance.OnSettingsOpened();
			SetUIFromSettings(lastAppliedSettings);
		}
	}

	protected override void OnMenuClosed()
	{

		if (Application.isPlaying)
		{
			//RebindManager.Instance.close();
			ApplySettings(lastAppliedSettings);
		}
	}

	void UpdateAudioVolume()
	{
		UpdateAudioVolume(masterVolumeSlider.value, musicVolumeSlider.value, sfxVolumeSlider.value);
	}

	void UpdateAudioVolume(float masterVolumeT, float musicVolumeT, float sfxVolumeT)
	{
		audioMixer.SetFloat("Master Volume", CalculateVolumeDB(masterVolumeT));
		audioMixer.SetFloat("Music Volume", CalculateVolumeDB(musicVolumeT));
		audioMixer.SetFloat("SFX Volume", CalculateVolumeDB(sfxVolumeT));
	}

	float CalculateVolumeDB(float volumeT)
	{
		// See https://www.dr-lex.be/info-stuff/volumecontrols.html
		return Mathf.Log10(Mathf.Lerp(0.0001f, 1, volumeT)) * 20;
	}

	void OnValidate()
	{
		if (!Application.isPlaying)
		{
			if (tabGroup != null)
			{
				tabGroup.ShowTab((int)defaultTab);
			}
		}
	}



}


public struct Settings
{
	public enum TerrainQuality { Low, High }
	public enum ShadowQuality { Disabled, Low, High }

	// Graphics
	public Vector2Int screenSize;
	public bool isFullscreen;
	public bool vsyncEnabled;
	public TerrainQuality terrainQuality;
	public ShadowQuality shadowQuality;

	// Audio / Language
	public string languageID;
	public float masterVolume;
	public float musicVolume;
	public float sfxVolume;

	// Load settings from prefs
	public static Settings LoadSavedSettings()
	{
		Settings settings = new Settings();
		// Graphics
		settings.vsyncEnabled = PlayerPrefs.GetInt(nameof(vsyncEnabled), defaultValue: 1) == 1;
		// Note: since Unity remembers screen size / fullscreen mode automatically, just get current screen size
		settings.screenSize = new Vector2Int(Screen.width, Screen.height);
		settings.isFullscreen = Screen.fullScreen;
		settings.terrainQuality = (TerrainQuality)PlayerPrefs.GetInt(nameof(terrainQuality), defaultValue: (int)TerrainQuality.High);
		settings.shadowQuality = (ShadowQuality)PlayerPrefs.GetInt(nameof(shadowQuality), defaultValue: (int)ShadowQuality.High);

		// Audio / Language
		settings.languageID = PlayerPrefs.GetString(nameof(languageID));
		settings.masterVolume = PlayerPrefs.GetFloat(nameof(masterVolume), defaultValue: 0.75f);
		settings.musicVolume = PlayerPrefs.GetFloat(nameof(musicVolume), defaultValue: 0.75f);
		settings.sfxVolume = PlayerPrefs.GetFloat(nameof(sfxVolume), defaultValue: 0.75f);
		return settings;
	}

	public static void Save(Settings settings)
	{
		// --- Graphics
		// Note: Unity remembers screen size / fullscreen mode automatically, so don't need to save these
		PlayerPrefs.SetInt(nameof(vsyncEnabled), (settings.vsyncEnabled) ? 1 : 0);
		PlayerPrefs.SetInt(nameof(terrainQuality), (int)settings.terrainQuality);
		PlayerPrefs.SetInt(nameof(shadowQuality), (int)settings.shadowQuality);

		// Audio / Language
		PlayerPrefs.SetString(nameof(languageID), settings.languageID);
		PlayerPrefs.SetFloat(nameof(masterVolume), settings.masterVolume);
		PlayerPrefs.SetFloat(nameof(musicVolume), settings.musicVolume);
		PlayerPrefs.SetFloat(nameof(sfxVolume), settings.sfxVolume);

		// Write
		PlayerPrefs.Save();
	}


}