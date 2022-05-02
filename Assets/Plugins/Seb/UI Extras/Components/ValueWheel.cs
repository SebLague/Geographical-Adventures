using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//
[ExecuteInEditMode]
public class ValueWheel : MonoBehaviour
{

	public event System.Action<int> onValueChanged;
	public string[] values;
	public int activeValueIndex;

	[SerializeField] Button decreaseButton;
	[SerializeField] Button increaseButton;
	[SerializeField] TMP_Text valueLabel;
	[SerializeField] RectTransform valueBox;
	[SerializeField] int width = 100;
	int widthOld;

	void Start()
	{
		if (Application.isPlaying)
		{
			decreaseButton.onClick.AddListener(() => MoveIndex(-1));
			increaseButton.onClick.AddListener(() => MoveIndex(+1));
			UpdateDisplayValue();
		}
	}

	void Update()
	{
		if (!Application.isPlaying)
		{
			if (widthOld != width)
			{
				widthOld = width;
				valueBox.sizeDelta = new Vector2(width, valueBox.sizeDelta.y);
				decreaseButton.GetComponent<RectTransform>().localPosition = new Vector3(-valueBox.sizeDelta.x / 2, 0);
				increaseButton.GetComponent<RectTransform>().localPosition = new Vector3(valueBox.sizeDelta.x / 2, 0);
			}
		}
	}

	void MoveIndex(int direction)
	{

		int oldIndex = activeValueIndex;
		activeValueIndex += direction;
		activeValueIndex = Mathf.Clamp(activeValueIndex, 0, values.Length - 1);
		
		if (oldIndex != activeValueIndex)
		{
			UpdateDisplayValue();
			onValueChanged?.Invoke(activeValueIndex);
		}
	}

	public void SetPossibleValues(string[] values, int activeIndex)
	{
		this.values = values;
		this.activeValueIndex = activeIndex;
		UpdateDisplayValue();
	}

	void UpdateDisplayValue()
	{
		decreaseButton.interactable = activeValueIndex > 0;
		increaseButton.interactable = activeValueIndex < values.Length - 1;

		if (values != null && values.Length > 0)
		{
			valueLabel.text = values[activeValueIndex];
		}
	}

}
