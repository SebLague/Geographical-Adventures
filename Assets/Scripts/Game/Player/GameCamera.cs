using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
	public event System.Action<Camera> gameCameraUpdateComplete;
	public enum ViewMode { TopDown, LookingForward, LookingBehind, MainMenu }
	public ViewMode activeView = ViewMode.LookingForward;

	public float fovSlow = 60;
	public float fovFast = 65;
	public float fovBoost = 75;
	public float fovSmoothTime;

	[Header("Top-Down Settings")]
	public float distanceAbove = 2;
	public float turnSpeed;
	public float startAngle;

	float angle;

	[Header("Alternate View Settings")]
	public ViewSettings lookingAheadView;
	public ViewSettings lookingBehindView;
	public ViewSettings menuView;

	[Header("References")]
	public Camera cam;


	// Other
	public Player player;
	Transform target;
	float smoothFovVelocity;

	float menuToGameViewTransitionT;


	void Start()
	{
		InitView();
	}

	public void SetActiveView(ViewMode viewMode) {
		activeView = viewMode;
	}


	public void InitView()
	{
		target = player.transform;
		UpdateView();
		cam.fieldOfView = CalculateFOV();
	}


	void LateUpdate()
	{
		if (!GameController.IsState(GameState.Paused))
		{
			UpdateView();

			cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, CalculateFOV(), ref smoothFovVelocity, fovSmoothTime);


			gameCameraUpdateComplete?.Invoke(cam);
		}
	}

	void UpdateView()
	{
		// Automatically swtich to menu cam if in main menu
		if (GameController.CurrentState == GameState.InMainMenu)
		{
			menuToGameViewTransitionT = 0;
			activeView = ViewMode.MainMenu;
		}

		// Set camera based on active view
		switch (activeView)
		{
			case ViewMode.TopDown:
				UpdateTopDownView();
				break;
			case ViewMode.LookingForward:
				UpdateAlternateView(lookingAheadView);
				break;
			case ViewMode.LookingBehind:
				UpdateAlternateView(lookingBehindView);
				break;
			case ViewMode.MainMenu:
				ViewSettings view = menuView;
				if (GameController.IsState(GameState.Playing))
				{
					// If started playing, transition from menu cam to forward cam
					menuToGameViewTransitionT += Time.deltaTime * 1f;
					view = ViewSettings.Lerp(menuView, lookingAheadView, Seb.Ease.Quadratic.Out(menuToGameViewTransitionT));
					if (menuToGameViewTransitionT > 1)
					{
						activeView = ViewMode.LookingForward;
					}
				}
				UpdateAlternateView(view);
				break;
		}
	}

	float CalculateFOV()
	{
		return (player.IsBoosting) ? fovBoost : Mathf.Lerp(fovSlow, fovFast, player.SpeedT);
	}

	void UpdateTopDownView()
	{

		if (player.worldIsSpherical)
		{
			transform.position = target.position.normalized * (player.Height + distanceAbove);
		}
		else
		{
			transform.position = new Vector3(target.position.x, player.Height + distanceAbove, target.position.z);
		}

		transform.position = target.position + player.GravityUp * distanceAbove;

		Vector3 gravityUp = player.GravityUp;
		transform.LookAt(target.position, target.forward);
		transform.RotateAround(transform.position, gravityUp, -player.totalTurnAngle + angle + startAngle);

		if (Input.GetKey(KeyBindings.TopDownCamTurnLeft))
		{
			angle -= turnSpeed * Time.smoothDeltaTime;
		}
		if (Input.GetKey(KeyBindings.TopDownCamTurnRight))
		{
			angle += turnSpeed * Time.smoothDeltaTime;
		}
	}

	void UpdateAlternateView(ViewSettings view)
	{
		// Calculate new position
		Vector3 newPos = target.position + target.forward * view.offset.z + player.GravityUp * view.offset.y;

		//Calculate look target
		Vector3 lookTarget = target.position;
		lookTarget += target.right * view.lookTargetOffset.x;
		lookTarget += target.up * view.lookTargetOffset.y;
		lookTarget += target.forward * view.lookTargetOffset.z;

		transform.position = newPos;
		transform.LookAt(lookTarget, player.GravityUp);
	}

	public bool topDownMode
	{
		get
		{
			return activeView == ViewMode.TopDown;
		}
	}

	[System.Serializable]
	public struct ViewSettings
	{
		public Vector3 offset;
		public Vector3 lookTargetOffset;

		public ViewSettings(Vector3 offset, Vector3 lookTargetOffset)
		{
			this.offset = offset;
			this.lookTargetOffset = lookTargetOffset;
		}

		public static ViewSettings Lerp(ViewSettings a, ViewSettings b, float t)
		{
			Vector3 offset = Vector3.Lerp(a.offset, b.offset, t);
			Vector3 lookTargetOffset = Vector3.Lerp(a.lookTargetOffset, b.lookTargetOffset, t);
			return new ViewSettings(offset, lookTargetOffset);
		}
	}

}