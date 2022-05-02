using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabGroup : MonoBehaviour
{
	public Tab[] tabs;
	public int currentTabIndex;

	void Start()
	{
		for (int i = 0; i < tabs.Length; i++)
		{
			int tabIndex = i;
			tabs[i].button.onClick.AddListener(() => ShowTab(tabIndex));
		}
		ShowTab(currentTabIndex);
	}

	public void ShowTab(int tabIndex)
	{
		currentTabIndex = Mathf.Clamp(tabIndex, 0, tabs.Length - 1);
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i].holder != null)
			{
				tabs[i].holder.SetActive(i == tabIndex);
			}
			if (tabs[i].button != null)
			{
				tabs[i].button.interactable = i != tabIndex;
			}
		}
	}

	[System.Serializable]
	public struct Tab
	{
		public Button button;
		public GameObject holder;
	}

	void OnValidate()
	{
		if (tabs != null)
		{

			ShowTab(currentTabIndex);
		}
	}
}
