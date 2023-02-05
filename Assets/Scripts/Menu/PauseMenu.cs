using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : Menu
{
    public Button quitButton;
    [SerializeField] PlayerAudio playerAudio;

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
        ToggleSound(false);
        GameController.SetPauseState(true);
    }

    protected override void OnMenuClosed()
    {
        base.OnMenuClosed();
        ToggleSound(true);
        GameController.SetPauseState(false);
    }

    private void ToggleSound(bool toggleSound)
    {
        playerAudio.gameObject.SetActive(toggleSound);
    }
}
