using System.Collections;
using System.Collections.Generic;
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
	public CanvasGroup group;

	public float smoothT;
	float smoothV;


	void Awake()
	{
		group.alpha = 0;
	}

	void Update()
	{
		bool uiIsActive = GameController.IsState(GameState.Playing);

		group.alpha = Mathf.SmoothDamp(group.alpha, uiIsActive ? 1 : 0, ref smoothV, smoothT);

		if (GameController.IsState(GameState.Playing))
		{
			// Map
			if (Input.GetKeyDown(KeyBindings.ToggleMap))
			{
				ToggleMapDisplay();
			}
		}
		if (GameController.IsState(GameState.Playing) || GameController.IsState(GameState.Paused))
		{
			if (Input.GetKeyDown(KeyBindings.TogglePause) || Input.GetKeyDown(KeyBindings.Escape))
			{
				pauseMenu.TogglePauseMenu();
			}
		}
	}


	public void ToggleMapDisplay()
	{
		bool showMap = map.ToggleActive(player);

		Seb.Helpers.GameObjectHelper.SetActiveAll(!showMap, hideInMapView);
	}
}
