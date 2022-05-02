using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
	public event System.Action menuClosedEvent;

	public GameObject menuHolder;
	public Button closeButton;
	public SubMenu[] subMenus;


	protected virtual void Awake()
	{
		if (closeButton != null)
		{
			closeButton.onClick.AddListener(CloseMenu);
		}
		if (subMenus != null)
		{
			foreach (var submenu in subMenus)
			{
				submenu.openButton.onClick.AddListener(submenu.menu.OpenMenu);
				submenu.openButton.onClick.AddListener(OnSubMenuOpened);
				submenu.menu.menuClosedEvent += OnSubMenuClosed;
				// If the menu is closed while one of its submenus is open, the submenu should be closed as well
				menuClosedEvent += submenu.menu.CloseMenu;
			}
		}

		// If open at start, then trigger OnOpened so any needed setup code can be run
		if (IsOpen)
		{
			OnMenuOpened();
		}
	}

	[NaughtyAttributes.Button()]
	public void OpenMenu()
	{
		if (!IsOpen)
		{
			menuHolder.SetActive(true);
			OnMenuOpened();
		}
	}
	[NaughtyAttributes.Button()]
	public void CloseMenu()
	{
		if (IsOpen)
		{
			menuClosedEvent?.Invoke();
			menuHolder.SetActive(false);
			OnMenuClosed();
		}
	}

	protected virtual void OnMenuOpened()
	{

	}

	protected virtual void OnMenuClosed()
	{

	}

	protected virtual void OnSubMenuOpened()
	{

	}

	protected virtual void OnSubMenuClosed()
	{

	}

	public bool IsOpen
	{
		get
		{
			return menuHolder.activeSelf;
		}
	}

	[System.Serializable]
	public struct SubMenu
	{
		public Menu menu;
		public Button openButton;
	}
}
