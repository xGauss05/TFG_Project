using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] TextMeshProUGUI PlayerFeedback;

    public void JoinLobby()
    {
        MainScreen.SetActive(false);
        JoinLobbyScreen.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        MainScreen.SetActive(true);
        JoinLobbyScreen.SetActive(false);
        PlayerFeedback.text = "";
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
