using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
	InMainMenu,
	Playing,
	ViewingMap,
	Paused,
	GameOver
}

public class GameController : MonoBehaviour
{
	public event System.Action onGameStarted;

	// Inspector variables
	[SerializeField] GameState startupState;
	[SerializeField] bool allowDevModeToggleInBuild;
	[SerializeField] MainMenu mainMenu;
	[SerializeField] Menu statsMenu;

	[Header("Debug")]
	[SerializeField] GameState debug_currentState;

	Stack<GameState> stateStack;

	// Internal variables
	static GameController _instance;
	bool devModeEnabledInBuild;


	void Awake()
	{
		stateStack = new Stack<GameState>();
		stateStack.Push(startupState);
	}

	void Start()
	{
		if (IsState(GameState.InMainMenu))
		{
			mainMenu.OpenMenu();
		}
		else if (IsState(GameState.Playing))
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

		debug_currentState = stateStack.Peek();

	}

	public static void GameOver()
	{
		if (!IsState(GameState.GameOver))
		{
			Time.timeScale = 0;
			SetState(GameState.GameOver);
			Instance.statsMenu.OpenMenu();
		}
	}

	public static void SwitchToEndlessMode()
	{
		Time.timeScale = 1;
		ReturnToPreviousState();
	}

	public static void SetPauseState(bool paused)
	{
		if (IsAnyState(GameState.Playing, GameState.ViewingMap, GameState.Paused))
		{
			Time.timeScale = (paused) ? 0 : 1;
			if (paused)
			{
				SetState(GameState.Paused);
			}
			else
			{
				ReturnToPreviousState();
			}
		}
		else
		{
			Debug.Log($"Cannot set pause state when current game state = {CurrentState}");
		}
	}

	static void ReturnToPreviousState()
	{
		if (Instance.stateStack.Count > 0)
		{
			Instance.stateStack.Pop();
		}
		else
		{
			Debug.Log("No previous state to return to... Something went wrong.");
			SetState(GameState.InMainMenu);
		}
	}

	public static void TogglePauseState()
	{
		bool pause = CurrentState == GameState.Playing;
		SetPauseState(pause);
	}

	public static void StartGame()
	{
		SetState(GameState.Playing);
		Instance.onGameStarted?.Invoke();
	}

	public static void SetState(GameState newState)
	{
		if (newState != CurrentState)
		{
			Instance.stateStack.Push(newState);
		}
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
			return Instance.stateStack.Peek();
		}
	}

	public static bool IsState(GameState state)
	{
		return CurrentState == state;
	}

	public static bool IsAnyState(params GameState[] states)
	{
		foreach (var state in states)
		{
			if (CurrentState == state)
			{
				return true;
			}
		}
		return false;
	}

	public static GameController Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<GameController>(includeInactive: true);
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
