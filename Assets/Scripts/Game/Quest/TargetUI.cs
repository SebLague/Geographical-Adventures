using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TargetUI : MonoBehaviour
{
	public Transform holder;
	public TMP_Text countryNameUI;
	public TMP_Text cityNameUI;
	public RectTransform numHolder;
	public TMP_Text numUI;
	public UnityEngine.UI.Image numBG;


	Color highlightCol = new Color(1, 0.38f, 0.33f, 1);
	Color numUICol;

	void Awake()
	{
		//holder.gameObject.SetActive(false);
		numBG.material = Instantiate(numBG.material);
		numUICol = numUI.color;
		ResetColours();
	}



	public void Set(string countryName, string cityName, string numDisplay)
	{
		countryNameUI.text = countryName;
		cityNameUI.text = cityName;
		holder.gameObject.SetActive(true);

		countryNameUI.ForceMeshUpdate();
		var bounds = countryNameUI.textBounds;
		numHolder.localPosition = new Vector3(bounds.center.x - (bounds.size.x / 2 + 50), numHolder.localPosition.y, 0);
		numUI.text = numDisplay;
	}

	public void Highlight()
	{
		countryNameUI.color = highlightCol;
		cityNameUI.color = highlightCol;
		numBG.color = highlightCol;
		numUI.color = Color.black;
	}

	public void ResetColours()
	{
		countryNameUI.color = Color.white;
		cityNameUI.color = Color.white;
		numBG.color = new Color(1, 1, 1, numBG.color.a);
		numUI.color = numUICol;
	}


}
