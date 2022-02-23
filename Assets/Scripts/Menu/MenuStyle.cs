using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Menu Style")]
public class MenuStyle : ScriptableObject
{
	[Header("Default Button Settings")]
	public ColorBlock buttonColours;
	public int buttonFontSize;

	public void ApplyButtonTheme(params Button[] buttons)
	{
		foreach (Button button in buttons)
		{
			button.colors = buttonColours;
			button.GetComponentInChildren<TMPro.TMP_Text>().fontSize = buttonFontSize;
		}
	}
}
