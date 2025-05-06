using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] TextMeshProUGUI PlayerFeedback;
    [SerializeField] AudioClip buttonSfx;

    public void JoinLobby()
    {
        MainScreen.SetActive(false);
        JoinLobbyScreen.SetActive(true);
        SFXManager.Singleton.PlaySound(buttonSfx);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void ReturnToMainMenu()
    {
        MainScreen.SetActive(true);
        JoinLobbyScreen.SetActive(false);
        SFXManager.Singleton.PlaySound(buttonSfx);
        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }

    public void ExitGame()
    {
        SFXManager.Singleton.PlaySound(buttonSfx);
        Application.Quit();
    }
}
 