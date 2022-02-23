using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MainMenu : MonoBehaviour
{

	public Button archetype;

	public Button playButton;
	public Button settingsButton;
	public Button aboutButton;
	public Button quitButton;

	AsyncOperation asyncGameLoadOperation;


	void Start()
	{
		if (Application.isPlaying)
		{
			playButton.onClick.AddListener(PlayGame);
			settingsButton.onClick.AddListener(() => MenuManager.OpenMenu(MenuManager.MenuType.Settings));
			aboutButton.onClick.AddListener(() => MenuManager.OpenMenu(MenuManager.MenuType.About));
			quitButton.onClick.AddListener(Quit);

			asyncGameLoadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1);
			asyncGameLoadOperation.allowSceneActivation = false;
		}
	}


	void PlayGame()
	{
		asyncGameLoadOperation.allowSceneActivation = true;
		//UnityEngine.SceneManagement.SceneManager.LoadScene(1);
	}

	void Update()
	{
		if (!Application.isPlaying)
		{
			var archetypeLabel = archetype.GetComponentInChildren<TMPro.TMP_Text>();
			foreach (Button b in GetAllButtons())
			{
				var label = b.GetComponentInChildren<TMPro.TMP_Text>();
				label.fontSize = archetypeLabel.fontSize;
				b.colors = archetype.colors;
				b.name = label.text + " Button";
			}
		}
	}

	Button[] GetAllButtons()
	{
		return new Button[] { playButton, settingsButton, aboutButton, quitButton };
	}

	void Quit()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

}
