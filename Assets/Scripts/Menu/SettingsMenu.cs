using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{

	public Vector2Int[] supportedRatios;

	List<Resolution> supportedResolutions;

	[Header("References")]
	public TMPro.TMP_Dropdown resolutionDropdown;
	public Toggle fullscreenToggle;



	void Awake()
	{
		InitResolutionSettings();
	}


	void InitResolutionSettings()
	{

		var optionsList = new List<TMP_Dropdown.OptionData>();
		supportedResolutions = new List<Resolution>();
		int currentResolutionIndex = -1;

		var monitorResolutions = Screen.resolutions;
		for (int i = 0; i < monitorResolutions.Length; i++)
		{
			var res = monitorResolutions[i];
			if (SupportedAspectRatio(res))
			{
				supportedResolutions.Add(res);
				optionsList.Add(new TMP_Dropdown.OptionData(ResolutionToName(res)));

				if (res.width == Screen.width && res.height == Screen.height)
				{
					currentResolutionIndex = supportedResolutions.Count - 1;
				}
			}
		}

		// Current screen resolution doesn't match any of the presets
		if (currentResolutionIndex == -1)
		{
			optionsList.Add(new TMP_Dropdown.OptionData($"Other ({Screen.width} x {Screen.height})"));
			currentResolutionIndex = optionsList.Count - 1;
			supportedResolutions.Add(Screen.currentResolution);
		}

		resolutionDropdown.ClearOptions();
		resolutionDropdown.AddOptions(optionsList);

		resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
		resolutionDropdown.onValueChanged.RemoveAllListeners();
		resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

		// Set up fullscreen toggle
		fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
		fullscreenToggle.onValueChanged.RemoveAllListeners();
		fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
	}


	void OnFullscreenChanged(bool isFullscreen)
	{
		FullScreenMode mode = (isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		Screen.SetResolution(Screen.width, Screen.height, mode);
	}

	void OnResolutionChanged(int index)
	{
		var res = supportedResolutions[index];
		Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
		Debug.Log(res.width + " " + res.height);
		//SettingsManager.UpdateResolution(supportedResolutions[index].width, supportedResolutions[index].height);
	}


	static Vector2Int GetRatio(int width, int height)
	{
		int gcd = GreatestCommonDivisor(width, height);
		int aspectW = width / gcd;
		int aspectH = height / gcd;
		Vector2Int aspect = new Vector2Int(aspectW, aspectH);
		return aspect;
	}

	static Vector2Int GetRatio(Resolution res)
	{
		return GetRatio(res.width, res.height);
	}

	public bool SupportedAspectRatio(Resolution res)
	{
		Vector2Int aspectRatio = GetRatio(res);
		foreach (var supportedAspect in supportedRatios)
		{
			if (aspectRatio == GetRatio(supportedAspect.x, supportedAspect.y))
			{
				return true;
			}
		}
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

	// Thanks to https://stackoverflow.com/a/41766138
	static int GreatestCommonDivisor(int a, int b)
	{
		while (a != 0 && b != 0)
		{
			if (a > b)
				a %= b;
			else
				b %= a;
		}

		return a | b;
	}
}
