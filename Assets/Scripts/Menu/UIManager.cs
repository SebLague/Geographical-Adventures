using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{

	public Player player;
	public GameObject game;
	public PauseMenu pauseMenu;
	public GameObject gameUI;
	public GameObject[] hideInMapView;
	public MapMenu map;
	public Compass compass;
	public CanvasGroup hudGroup;

	public float smoothT;
	float smoothV;

	public GameObject controlTab;
	public GameObject[] keybindButtons;


	void Awake()
	{
		hudGroup.alpha = 0;
		
		controlTab.SetActive(true);
		keybindButtons = GameObject.FindGameObjectsWithTag("Keybind");
		controlTab.SetActive(false);
	}

	void Update()
	{
		bool uiIsActive = GameController.IsState(GameState.Playing) || GameController.IsState(GameState.ViewingMap);

		hudGroup.alpha = Mathf.SmoothDamp(hudGroup.alpha, uiIsActive ? 1 : 0, ref smoothV, smoothT);


	}

	public void ToggleMap()
	{
		if (GameController.IsAnyState(GameState.Playing, GameState.ViewingMap))
		{
			ToggleMapDisplay();
		}
	}

	public void TogglePause()
	{
		if (GameController.IsAnyState(GameState.Playing, GameState.ViewingMap, GameState.Paused))
		{
			pauseMenu.TogglePauseMenu();
		}
	}


	public void ToggleMapDisplay()
	{
		bool showMap = map.ToggleActive(player);
		if (showMap)
		{
			GameController.SetState(GameState.ViewingMap);
		}
		else
		{
			GameController.SetState(GameState.Playing);
		}

		Seb.Helpers.GameObjectHelper.SetActiveAll(!showMap, hideInMapView);
	}

	public void UpdateKeyText(string key, KeyCode code)
	{
		var tmp = Array.Find(keybindButtons, x => x.name == key);
		if (tmp != null)
		{
			var txt = tmp.GetComponentInChildren<TMP_Text>();
			txt.text = code.ToString();
		}
	}
}