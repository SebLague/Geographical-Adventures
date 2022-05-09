using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: migrate these last few controls to new input system and delete this class
public static class KeyBindings
{

	public static KeyCode TopDownCamTurnLeft = KeyCode.LeftArrow;
	public static KeyCode TopDownCamTurnRight = KeyCode.RightArrow;
	public static KeyCode Escape = KeyCode.Escape;

	// ----- Dev Mode -----
	public static KeyCode ToggleDevMode = KeyCode.LeftBracket;
	public static KeyCode Debug_ToggleLockPlayer = KeyCode.L;
}
