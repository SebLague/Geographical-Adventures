using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
	public GameObject pauseMenu;
	public Button resumeButton;
	public Button menuButton;
	public Button quitButton;
	public MenuStyle style;
	bool paused;

	void Start()
	{
		ApplyTheme();
		resumeButton.onClick.AddListener(() => SetPauseState(false));
		menuButton.onClick.AddListener(ReturnToMainMenu);
		quitButton.onClick.AddListener(() => Application.Quit());
		SetPauseState(false);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
		{
			TogglePause();
		}
	}

	void ReturnToMainMenu()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}

	void TogglePause()
	{
		SetPauseState(!paused);
	}

	void SetPauseState(bool paused)
	{
		this.paused = paused;
		Time.timeScale = (paused) ? 0 : 1;
		pauseMenu.SetActive(paused);
	}

	void ApplyTheme()
	{
		style.ApplyButtonTheme(resumeButton, menuButton, quitButton);
	}

}
