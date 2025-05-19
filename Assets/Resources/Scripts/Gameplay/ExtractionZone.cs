using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ExtractionZone : MonoBehaviour
{
    public int playersInside;
    public int requiredPlayers;

    public UnityEvent onExtractionFull = new UnityEvent();

    void Start()
    {
        requiredPlayers = LobbyReference.Singleton.currentLobby.Value.MemberCount;

        LobbyReference.Singleton.onLobbyUpdated.AddListener(UpdatedRequiredPlayers);

        onExtractionFull.AddListener(CheckExtraction);
    }

    void OnDestroy()
    {
        if (LobbyReference.Singleton != null)
        {
            LobbyReference.Singleton.onLobbyUpdated.RemoveListener(UpdatedRequiredPlayers);
        }
    }

    public void UpdatedRequiredPlayers()
    {
        requiredPlayers = LobbyReference.Singleton.currentLobby.Value.MemberCount;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        playersInside++;

        onExtractionFull?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        playersInside--;

        onExtractionFull?.Invoke();
    }

    void CheckExtraction()
    {
        if (playersInside >= requiredPlayers)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Single);
            NetworkManager.Singleton.Shutdown();
        }
    }
}
