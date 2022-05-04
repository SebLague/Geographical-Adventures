using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{

	public Player player;
	public GeoGame.Quest.QuestSystem questSystem;
	public GameCamera gameCamera;
	public UIManager uIManager;
	PlayerAction playerActions;


	void Awake()
	{
		playerActions = new PlayerAction();
	}

	void OnEnable()
	{
		playerActions.PlayerControls.Enable();
		playerActions.CameraControls.Enable();
		playerActions.UIControls.Enable();
	}

	void Update()
	{
		if (GameController.IsState(GameState.Playing))
		{
			PlayerControls();
			CameraControls();
		}

		UIControls();
	}

	void PlayerControls()
	{
		Vector2 movementInput = playerActions.PlayerControls.Movement.ReadValue<Vector2>();
		float accelerateDir = playerActions.PlayerControls.Speed.ReadValue<float>();
		bool boosting = playerActions.PlayerControls.Boost.IsPressed();
		player.UpdateMovementInput(movementInput, accelerateDir, boosting);


		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("dropPackage")))
		{
			questSystem.TryDropPackage();
		}
	}

	void CameraControls()
	{
		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("CameraView1")))
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingForward);
		}
		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("CameraView2")))
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingBehind);
		}
		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("CameraView3")))
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.TopDown);
		}
	}

	void UIControls()
	{
		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("TogglePause")))
		{
			uIManager.TogglePause();
		}

		if (Input.GetKeyDown(KeyBindings.Instance.GetKey("ToggleMap")))
		{
			uIManager.ToggleMap();
		}
	}

}
