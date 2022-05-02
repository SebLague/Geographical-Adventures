using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

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

	[Header("Audio Settings")]
	public Slider masterVolumeSlider;
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;
	public AudioMixer audioMixer;


	[Header("Other References")]
	public TabGroup tabGroup;
	public Button applyButton;

	// Private stuff
	Dictionary<Vector2Int, List<Vector2Int>> supportedResolutions;
	Settings lastAppliedSettings;


	void Start()
	{
		AddListeners();
		ApplySettings(Settings.LoadSavedSettings());

	}

	void AddListeners()
	{
		applyButton.onClick.AddListener(ApplyCurrentSettings);


		masterVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());
		musicVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());
		sfxVolumeSlider.onValueChanged.AddListener((volume) => UpdateAudioVolume());
	}

	void SetUIFromSettings(Settings settings)
	{
		// Graphics
		fullscreenToggle.SetIsOnWithoutNotify(settings.isFullscreen);
		vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);
		InitResolutionSettings(settings.screenSize);
		// Music
		masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
		musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
		sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

	}

	Settings GetSettingsFromUI()
	{
		Settings settings = new Settings();
		settings.isFullscreen = fullscreenToggle.isOn;
		settings.screenSize = GetCurrentResolutionOptions()[resolutionWheel.activeValueIndex];
		settings.vsyncEnabled = vsyncToggle.isOn;
		settings.masterVolume = masterVolumeSlider.value;
		settings.sfxVolume = sfxVolumeSlider.value;
		settings.musicVolume = musicVolumeSlider.value;
		return settings;
	}

	void ApplyCurrentSettings()
	{
		Settings currentSettings = GetSettingsFromUI();
		ApplySettings(currentSettings);
	}

	// Applies the settings and saves them to disc
	void ApplySettings(Settings settings)
	{
		// Apply audio settings
		UpdateAudioVolume(settings.masterVolume, settings.musicVolume, settings.sfxVolume);

		// Apply graphics settings
		FullScreenMode mode = (settings.isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		Screen.SetResolution(settings.screenSize.x, settings.screenSize.y, mode);
		QualitySettings.vSyncCount = (settings.vsyncEnabled) ? 1 : 0;

		// Save
		lastAppliedSettings = settings;
		Settings.Save(settings);
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

	void OnResolutionChanged(int index)
	{
		//var res = supportedResolutions[index];
		//Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
		//Debug.Log(res.width + " " + res.height);
		//SettingsManager.UpdateResolution(supportedResolutions[index].width, supportedResolutions[index].height);
	}


	static Vector2Int GetRatio(int width, int height)
	{
		int gcd = Maths.Other.GreatestCommonDivisor(width, height);
		int aspectW = width / gcd;
		int aspectH = height / gcd;
		Vector2Int aspect = new Vector2Int(aspectW, aspectH);
		return aspect;
	}

	static Vector2Int GetRatio(Resolution res)
	{
		return GetRatio(res.width, res.height);
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

	string ResolutionToName(Resolution res)
	{
		Vector2Int aspectRatio = GetRatio(res);
		// Change 8:5 to 16:10 since that's typically used (presumably to compare easier to 16:9)
		if (aspectRatio.x == 8 && aspectRatio.y == 5)
		{
			aspectRatio *= 2;
		}
		return $"{res.width} x {res.height} ({aspectRatio.x}:{aspectRatio.y})";
	}

	protected override void OnMenuOpened()
	{
		if (Application.isPlaying)
		{
			lastAppliedSettings = Settings.LoadSavedSettings();
			SetUIFromSettings(lastAppliedSettings);
		}
	}

	protected override void OnMenuClosed()
	{
		if (Application.isPlaying)
		{
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

	public struct Settings
	{
		// Graphics
		public Vector2Int screenSize;
		public bool isFullscreen;
		public bool vsyncEnabled;

		// Audio
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

			// Audio
			settings.masterVolume = PlayerPrefs.GetFloat(nameof(masterVolume), defaultValue: 0.75f);
			settings.musicVolume = PlayerPrefs.GetFloat(nameof(musicVolume), defaultValue: 0.75f);
			settings.sfxVolume = PlayerPrefs.GetFloat(nameof(sfxVolume), defaultValue: 0.75f);
			return settings;
		}

		public static void Save(Settings settings)
		{
			//Debug.Log("Save volume: " + settings.masterVolume);
			// Note: Unity remembers screen size / fullscreen mode automatically, so don't need to save these
			PlayerPrefs.SetInt(nameof(vsyncEnabled), (settings.vsyncEnabled) ? 1 : 0);
			PlayerPrefs.SetFloat(nameof(masterVolume), settings.masterVolume);
			PlayerPrefs.SetFloat(nameof(musicVolume), settings.musicVolume);
			PlayerPrefs.SetFloat(nameof(sfxVolume), settings.sfxVolume);
			PlayerPrefs.Save();
		}


	}


}
