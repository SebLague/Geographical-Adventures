using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GeoGame.Localization;

public class CustomButton : Button
{

	public event System.Action onPointerEnter;
	public event System.Action onPointerExit;

	public StringLocalizer localizer;
	[Header("Settings")]
	//public string buttonText;
	public bool changeTextOnMouseOver;
	//public string mouseOverButtonText;

	[Header("References")]
	public TMPro.TMP_Text label;


	void SetLabel(string text)
	{
		if (label)
		{
			label.text = text;
		}
	}


	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		if (changeTextOnMouseOver)
		{
			SetLabel($"<   {localizer.currentValue}   >");
		}
		onPointerEnter?.Invoke();
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		SetLabel(localizer.currentValue);
		onPointerExit?.Invoke();
	}
}
