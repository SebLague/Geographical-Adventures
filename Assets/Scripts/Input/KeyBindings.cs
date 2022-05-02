using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KeyBindings
{
	// Player controls
	public static KeyCode turnLeftKey = KeyCode.A;
	public static KeyCode turnRightKey = KeyCode.D;
	public static KeyCode accelerateKey = KeyCode.E;
	public static KeyCode decelerateKey = KeyCode.Q;

	public static KeyCode pitchUpKey = KeyCode.W;
	public static KeyCode pitchDownKey = KeyCode.S;
	public static KeyCode boostkey = KeyCode.LeftShift;
	public static KeyCode dropPackageKey = KeyCode.Space;

	public static KeyCode fastForwardToDayTime = KeyCode.Return;
	public static KeyCode fastForwardToNightTime = KeyCode.Backspace;

	// Game UI controls
	public static KeyCode TogglePause = KeyCode.P;
	public static KeyCode ToggleMap = KeyCode.M;
	public static KeyCode Escape = KeyCode.Escape;

	// Game camera controls
	public static KeyCode CameraView1 = KeyCode.Alpha1;
	public static KeyCode CameraView2 = KeyCode.Alpha2;
	public static KeyCode CameraView3 = KeyCode.Alpha3;
	public static KeyCode TopDownCamTurnLeft = KeyCode.LeftArrow;
	public static KeyCode TopDownCamTurnRight = KeyCode.RightArrow;

	// ----- Dev Mode -----
	public static KeyCode ToggleDevMode = KeyCode.LeftBracket;
	public static KeyCode Debug_ToggleLockPlayer = KeyCode.L;
}
