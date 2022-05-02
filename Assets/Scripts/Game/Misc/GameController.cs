using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
	InMainMenu,
	Playing,
	Paused,
	GameOver
}

public class GameController : MonoBehaviour
{
	public event System.Action onGameStarted;

	// Inspector variables
	[SerializeField] GameState currentGameState;
	[SerializeField] bool allowDevModeToggleInBuild;
	[SerializeField] MainMenu mainMenu;
	[SerializeField] Menu statsMenu;

	// Internal variables
	static GameController _instance;
	bool devModeEnabledInBuild;

	void Start()
	{
		if (currentGameState == GameState.InMainMenu)
		{
			mainMenu.OpenMenu();
		}
		else if (currentGameState == GameState.Playing)
		{
			StartGame();
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyBindings.ToggleDevMode) && allowDevModeToggleInBuild)
		{
			devModeEnabledInBuild = !devModeEnabledInBuild;
		}

	}

	public static void GameOver()
	{
		if (!IsState(GameState.GameOver))
		{
			Time.timeScale = 0;
			Instance.currentGameState = GameState.GameOver;
			Instance.statsMenu.OpenMenu();
		}
	}

	public static void SwitchToEndlessMode()
	{
		Time.timeScale = 1;
		Instance.currentGameState = GameState.Playing;
	}

	public static void SetPauseState(bool paused)
	{
		if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
		{
			Time.timeScale = (paused) ? 0 : 1;
			Instance.currentGameState = (paused) ? GameState.Paused : GameState.Playing;
		}
		else
		{
			Debug.Log($"Cannot set pause state when current game state = {CurrentState}");
		}
	}

	public static void TogglePauseState()
	{
		bool pause = CurrentState == GameState.Playing;
		SetPauseState(pause);
	}

	public static void StartGame()
	{
		Instance.currentGameState = GameState.Playing;
		Instance.onGameStarted?.Invoke();
	}

	public static void ExitToMainMenu()
	{
		if (IsState(GameState.Paused))
		{
			SetPauseState(false);
		}
		SceneManager.LoadScene(0);
	}

	public static void Quit()
	{
		if (Application.isEditor)
		{
			ExitPlayMode();
		}
		else
		{
			Application.Quit();
		}

	}

	public static bool InDevMode
	{
		get
		{
			return Application.isEditor || Instance.devModeEnabledInBuild;
		}
	}

	public static GameState CurrentState
	{
		get
		{
			return Instance.currentGameState;
		}
	}

	public static bool IsState(GameState state)
	{
		return CurrentState == state;
	}

	public static GameController Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<GameController>();
			}
			return _instance;
		}
	}

	static void ExitPlayMode()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}
