using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControlSettings : MonoBehaviour
{

	public TMP_Text labels;
	public TMP_Text values;

	void Start()
	{
		Refresh();
	}

	[NaughtyAttributes.Button()]
	public void Refresh()
	{
		labels.text = "";
		values.text = "";
		/*
		AddHeader("Player Controls");
		Add("Turn", KeyCodeToName(KeyBindings.turnLeftKey) + " / " + KeyCodeToName(KeyBindings.turnRightKey));
		Add("Speed", KeyCodeToName(KeyBindings.decelerateKey) + " / " + KeyCodeToName(KeyBindings.accelerateKey));
		Add("Elevation", KeyCodeToName(KeyBindings.pitchUpKey) + " / " + KeyCodeToName(KeyBindings.pitchDownKey));
		Add("Boost", KeyCodeToName(KeyBindings.boostkey));
		Add("Drop Package", KeyCodeToName(KeyBindings.dropPackageKey));

		AddSpace();
		AddHeader("Camera Controls");
		Add("Look Down", KeyCodeToName(KeyBindings.CameraView1));
		Add("Look Forward", KeyCodeToName(KeyBindings.CameraView2));
		Add("Look Behind", KeyCodeToName(KeyBindings.CameraView3));

		AddSpace();
		AddHeader("Time Controls");
		Add("Fast Forward to Night", KeyCodeToName(KeyBindings.fastForwardToNightTime));
		Add("Fast Forward to Day", KeyCodeToName(KeyBindings.fastForwardToDayTime));

		AddSpace();
		AddHeader("UI");
		Add("Pause", KeyCodeToName(KeyBindings.TogglePause));
		*/

		AddHeader("Main Controls");
		Add("Turn", KeyCodeToName(KeyBindings.Instance.GetKey("turnLeft")) + " / " + KeyCodeToName(KeyBindings.Instance.GetKey("turnRight")));
		Add("Speed", KeyCodeToName(KeyBindings.Instance.GetKey("decelerate")) + " / " + KeyCodeToName(KeyBindings.Instance.GetKey("accelerate")));
		Add("Elevation", KeyCodeToName(KeyBindings.Instance.GetKey("pitchUp")) + " / " + KeyCodeToName(KeyBindings.Instance.GetKey("pitchDown")));
		Add("Boost", KeyCodeToName(KeyBindings.Instance.GetKey("boost")));
		Add("Drop Package", KeyCodeToName(KeyBindings.Instance.GetKey("dropPackage")));

		AddSpace();
		AddHeader("Extra Controls");
		Add("Change View", $"{KeyCodeToName(KeyBindings.Instance.GetKey("CameraView1"))} / {KeyCodeToName(KeyBindings.Instance.GetKey("CameraView2"))} / {KeyCodeToName(KeyBindings.Instance.GetKey("CameraView3"))}");
		Add("Fast Forward to Night", KeyCodeToName(KeyBindings.Instance.GetKey("fastForwardToNightTime")));
		Add("Fast Forward to Day", KeyCodeToName(KeyBindings.Instance.GetKey("fastForwardToDayTime")));

		AddSpace();
		AddHeader("UI");
		Add("Map", KeyCodeToName(KeyBindings.Instance.GetKey("ToggleMap")));
		Add("Pause", KeyCodeToName(KeyBindings.Instance.GetKey("TogglePause")));

	}

	void AddHeader(string header)
	{
		Add($"<color=#FFFFFF><space=-5><b>{header}</b></color>", " ");
		AddSpace(25);
	}

	void Add(string label, string value)
	{
		if (!string.IsNullOrEmpty(labels.text))
		{
			labels.text += '\n';
		}
		if (!string.IsNullOrEmpty(values.text))
		{
			values.text += '\n';
		}
		labels.text += label;
		values.text += value;
	}

	void AddSpace(int sizePercent = 100)
	{
		string lineBreak = $"<line-height={sizePercent}%>\n</line-height>";
		labels.text += lineBreak;
		values.text += lineBreak;
	}

	static string KeyCodeToName(KeyCode code)
	{
		string name = code.ToString();
		if (name.Contains("Alpha"))
		{
			name = name.Replace("Alpha", "");
		}
		if (name == "LeftShift")
		{
			name = "Shift";
		}

		return name;
	}
}
