using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] TextMeshProUGUI PlayerFeedback;
    [SerializeField] AudioClip buttonSfx;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ButtonPress()
    {
        SFXManager.Singleton.PlaySound(buttonSfx);
    }

    public void JoinLobby()
    {
        MainScreen.SetActive(false);
        JoinLobbyScreen.SetActive(true);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void ReturnToMainMenu()
    {
        MainScreen.SetActive(true);
        JoinLobbyScreen.SetActive(false);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void ExitGame()
    {
        SteamClient.Shutdown();
        Application.Quit();
    }
}
