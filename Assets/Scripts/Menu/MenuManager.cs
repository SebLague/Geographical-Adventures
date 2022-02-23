using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{

	public enum MenuType { Main, Settings, About }
	public MenuType activeMenu;
	public MenuStyle style;

	[Header("References")]
	public TMPro.TMP_Text version;
	public GameObject mainMenu;
	public GameObject settingsMenu;
	public GameObject aboutMenu;

	public Button[] backButtons;

	static MenuManager _instance;

	void Awake()
	{
		ApplyStyle();
		version.text = $"Version {Application.version}";
		foreach (var backButton in backButtons)
		{
			backButton.onClick.AddListener(() => OpenMenu(MenuType.Main));
		}
		OpenMenu(MenuType.Main);
	}

	public void ApplyStyle()
	{
		var allButtons = FindObjectsOfType<Button>(includeInactive: true);
		style.ApplyButtonTheme(allButtons);
	}

	static MenuManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<MenuManager>();
			}
			return _instance;
		}
	}

	public static void OpenMenu(MenuType menu)
	{
		// Hide all
		instance.mainMenu.SetActive(false);
		instance.settingsMenu.SetActive(false);
		instance.aboutMenu.SetActive(false);

		// Show chosen
		switch (menu)
		{
			case MenuType.Main:
				instance.mainMenu.SetActive(true);
				break;
			case MenuType.Settings:
				instance.settingsMenu.SetActive(true);
				break;
			case MenuType.About:
				instance.aboutMenu.SetActive(true);
				break;
		}
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		if (!Application.isPlaying)
		{
			OpenMenu(activeMenu);
		}
	}
#endif
}
