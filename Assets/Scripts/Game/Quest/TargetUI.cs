using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode]
public class TargetUI : MonoBehaviour
{
	public CanvasGroup holder;
	public TMP_Text countryNameUI;
	public TMP_Text cityNameUI;
	public RectTransform numHolder;
	public Image statusIcon;
	public Sprite completedSprite;
	RectTransform rectTransform;

	public bool debug;
	public bool set;


	Color highlightCol = new Color(1, 0.38f, 0.33f, 1);
	Color numUICol;

	void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	void Update()
	{
		if (debug)
		{
			float countryNameLeftEdge = countryNameUI.rectTransform.anchoredPosition.x;
			float countryNameCentreX = countryNameLeftEdge + countryNameUI.bounds.size.x / 2;
			Debug.Log(rectTransform.localPosition.x + "   " + rectTransform.sizeDelta.x);
			if (set)
			{
				set = false;
				Debug.Log("Set");
				UpdatePosAndSize();
				//cityNameUI.rectTransform.anchoredPosition = new Vector2(countryNameCentreX, cityNameUI.rectTransform.anchoredPosition.y);
			}
		}
	}

	public void MarkCompleted() {
		statusIcon.sprite = completedSprite;
		statusIcon.rectTransform.eulerAngles = Vector3.forward * 0;
		holder.alpha = 0.5f;
	}


	public void Set(string countryName, string cityName, bool isPickup)
	{
		countryNameUI.text = countryName;
		cityNameUI.text = cityName;
		statusIcon.rectTransform.eulerAngles = Vector3.forward * ((isPickup) ? 0 : 180);

		UpdatePosAndSize();
	}

	void UpdatePosAndSize()
	{
		// Force mesh update so text bounds is correct
		countryNameUI.ForceMeshUpdate();
		cityNameUI.ForceMeshUpdate();

		// Position city name
		float countryTextCentreX = countryNameUI.rectTransform.localPosition.x + countryNameUI.bounds.size.x / 2;
		cityNameUI.rectTransform.localPosition = new Vector2(countryTextCentreX, cityNameUI.rectTransform.localPosition.y);



		ResizeContentBoundsToFitText();
	}

	void ResizeContentBoundsToFitText()
	{


		float countryNameRightEdge = countryNameUI.rectTransform.anchoredPosition.x + countryNameUI.bounds.size.x; // pivot on left edge
																												   //float cityNameRightEdge = cityNameUI.rectTransform.anchoredPosition.x + cityNameUI.bounds.size.x / 2; // pivot in centre
		rectTransform.sizeDelta = new Vector2(countryNameRightEdge, rectTransform.sizeDelta.y);
		//var r = gameObject.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();

	}

	public RectTransform RectTransform
	{
		get
		{
			return rectTransform;
		}
	}



}
