using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button
{

	public event System.Action onPointerEnter;
	public event System.Action onPointerExit;

	[Header("Settings")]
	public string buttonText;
	public bool changeTextOnMouseOver;
	public string mouseOverButtonText;

	[Header("References")]
	public TMPro.TMP_Text label;

	bool pointerIsOver;


	public void ChangeLabel(string newLabel)
	{
		buttonText = newLabel;
		if (pointerIsOver || !changeTextOnMouseOver)
		{
			SetLabel(buttonText);
		}
	}

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
			SetLabel(mouseOverButtonText);
		}
		pointerIsOver = true;
		onPointerEnter?.Invoke();
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		pointerIsOver = false;
		SetLabel(buttonText);
		onPointerExit?.Invoke();
	}

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();
		SetLabel(buttonText);
	}
#endif
}
