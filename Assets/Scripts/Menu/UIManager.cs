using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	public Toggle invertInputToggle;

	public float smoothT;
	float smoothV;


	void Awake()
	{
		hudGroup.alpha = 0;
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
}
