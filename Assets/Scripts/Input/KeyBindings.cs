using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class KeyBindings : MonoBehaviour
{
	private static KeyBindings _instance;

	[SerializeField] private UIManager UIManager;

	public static KeyBindings Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<KeyBindings>();
			}

			return _instance;
		}
	}
	
	public Dictionary<string, KeyCode> MainBinds { get; private set; }
	public bool isPitchInverted;

	private string _bindName;

	private void Start()
	{
		MainBinds = new Dictionary<string, KeyCode>();
		
		BindAllDefaults();
	}

	public void BindKey(string key, KeyCode keyBind)
	{
		Debug.Log(key);
		Debug.Log(keyBind);
		
		if (!MainBinds.ContainsKey(key) && !MainBinds.ContainsValue(keyBind))
		{
			MainBinds.Add(key, keyBind);
		}
		else if (!MainBinds.ContainsKey(key) && MainBinds.ContainsValue(keyBind))
		{
			ClearKeycode(keyBind);
			
			MainBinds.Add(key, keyBind);
		}
		else if (MainBinds.ContainsKey(key) && MainBinds.ContainsValue(keyBind))
		{
			ClearKeycode(keyBind);
		}

		MainBinds[key] = keyBind;
		UIManager.UpdateKeyText(key, keyBind);
		_bindName = String.Empty;
	}

	private void ClearKeycode(KeyCode keyBind)
	{
		string myKey = MainBinds.FirstOrDefault(x => x.Value == keyBind).Key;

		MainBinds[myKey] = KeyCode.None;
		UIManager.UpdateKeyText(myKey, KeyCode.None);
	}
	
	public void KeybindOnClick(string bindName)
	{
		this._bindName = bindName;
	}

	private void OnGUI()
	{
		if (_bindName != String.Empty)
		{
			Event e = Event.current;

			if (e.isKey)
			{
				BindKey(_bindName, e.keyCode);
			}
		}
	}

	public KeyCode GetKey(string key)
	{
		return MainBinds.GetValueOrDefault(key);
	}

	private void BindAllDefaults()
	{
		// Player controls
		BindKey("turnLeft", KeyCode.A);
		BindKey("turnRight", KeyCode.D);
		BindKey("accelerate", KeyCode.E);
		BindKey("decelerate", KeyCode.Q);

		BindKey("pitchUp", KeyCode.W);
		BindKey("pitchDown", KeyCode.S);
		BindKey("boost", KeyCode.LeftShift);
		BindKey("dropPackage", KeyCode.Space);

		BindKey("fastForwardToDayTime", KeyCode.Return);
		BindKey("fastForwardToNightTime", KeyCode.Backspace);

		// Game UI controls
		BindKey("TogglePause", KeyCode.P);
		BindKey("ToggleMap", KeyCode.M);
		BindKey("Escape", KeyCode.Escape);

		// Game camera controls
		BindKey("CameraView1", KeyCode.Alpha1);
		BindKey("CameraView2", KeyCode.Alpha2);
		BindKey("CameraView3", KeyCode.Alpha3);
		BindKey("TopDownCamTurnLeft", KeyCode.LeftArrow);
		BindKey("TopDownCamTurnRight", KeyCode.RightArrow);

		// ----- Dev Mode -----
		BindKey("ToggleDevMode", KeyCode.LeftBracket);
		BindKey("Debug_ToggleLockPlayer", KeyCode.L);
	}

	public void InvertPitchInput()
	{
		isPitchInverted = !isPitchInverted;
	}
}
