using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : Menu
{

	public Button quitButton;
	public AudioSource planeAudio;
    
	void Start()
	{
		quitButton.onClick.AddListener(GameController.ExitToMainMenu);
	}


	public void TogglePauseMenu()
	{
		if (IsOpen)
		{
			CloseMenu();
		}
		else
		{
			OpenMenu();
		}
	}


	protected override void OnMenuOpened()
	{
		base.OnMenuOpened();
		GameController.SetPauseState(true);
		planeAudio.Pause();
	}

	protected override void OnMenuClosed()
	{
		base.OnMenuClosed();
		GameController.SetPauseState(false);
		planeAudio.Play();
	}

}
