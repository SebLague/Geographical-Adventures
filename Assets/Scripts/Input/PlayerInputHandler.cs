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
	public SolarSystem.SolarSystemManager solarSystemManager;
	PlayerAction playerActions;


	void Start()
	{
		playerActions = RebindManager.Instance.activePlayerActions;

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
			SolarSystemControls();
		}

		UIControls();
	}

	void PlayerControls()
	{
		Vector2 movementInput = playerActions.PlayerControls.Movement.ReadValue<Vector2>();
		float accelerateDir = playerActions.PlayerControls.Speed.ReadValue<float>();
		bool boosting = playerActions.PlayerControls.Boost.IsPressed();
		player.UpdateMovementInput(movementInput, accelerateDir, boosting);


		if (playerActions.PlayerControls.DropPackage.WasPressedThisFrame())
		{
			questSystem.TryDropPackage();
		}
	}

	void SolarSystemControls()
	{
		if (playerActions.PlayerControls.MakeDaytime.WasPressedThisFrame())
		{
			solarSystemManager.FastForward(toDaytime: true);
		}
		if (playerActions.PlayerControls.MakeNighttime.WasPressedThisFrame())
		{
			solarSystemManager.FastForward(toDaytime: false);
		}
	}

	void CameraControls()
	{
		if (playerActions.CameraControls.ForwardCameraView.WasPressedThisFrame())
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingForward);
		}
		if (playerActions.CameraControls.BackwardCameraView.WasPressedThisFrame())
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingBehind);
		}
		if (playerActions.CameraControls.TopCameraView.WasPressedThisFrame())
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.TopDown);
		}
	}

	void UIControls()
	{
		if (playerActions.UIControls.TogglePause.WasPressedThisFrame())
		{
			uIManager.TogglePause();
		}

		if (playerActions.UIControls.ToggleMap.WasPressedThisFrame())
		{
			uIManager.ToggleMap();
		}
	}

}
