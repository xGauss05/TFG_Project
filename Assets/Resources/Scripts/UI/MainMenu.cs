using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] GameObject HostConfirmationScreen;
    [SerializeField] GameObject RankingScreen;
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

    public void OpenHostConfirmation()
    {
        HostConfirmationScreen.SetActive(true);
    }

    public void CloseHostConfirmation()
    {
        HostConfirmationScreen.SetActive(false);
    }

    public void JoinLobby()
    {
        MainScreen.SetActive(false);
        JoinLobbyScreen.SetActive(true);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void Ranking()
    {
        RankingScreen.SetActive(true);
        MainScreen.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        MainScreen.SetActive(true);
        JoinLobbyScreen.SetActive(false);
        RankingScreen.SetActive(false);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void ExitGame()
    {
        SteamClient.Shutdown();
        Application.Quit();
    }
}
