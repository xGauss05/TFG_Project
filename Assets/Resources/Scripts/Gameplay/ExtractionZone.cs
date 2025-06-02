using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ExtractionZone : NetworkBehaviour
{
    public int playersInside;
    public int requiredPlayers;

    [SerializeField] Door safeDoor;

    void OnEnable()
    {
        SteamMatchmaking.OnLobbyMemberLeave += UpdatedRequiredPlayers;
    }

    void OnDisable()
    {
        SteamMatchmaking.OnLobbyMemberLeave -= UpdatedRequiredPlayers;
    }

    void Start()
    {
        if (IsClient)
        {
            this.enabled = false;
            return;
        }

        requiredPlayers = LobbyReference.Singleton.currentLobby.Value.MemberCount;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsClient) return;

        if (!other.CompareTag("Player")) { return; }

        playersInside++;

        CheckExtraction();
    }

    void OnTriggerExit(Collider other)
    {
        if (IsClient) return;

        if (!other.CompareTag("Player")) { return; }

        playersInside--;

        CheckExtraction();
    }

    void UpdatedRequiredPlayers(Lobby arg1, Friend arg2)
    {
        requiredPlayers = LobbyReference.Singleton.currentLobby.Value.MemberCount;
    }

    void CheckExtraction()
    {
        if (playersInside >= requiredPlayers && !safeDoor.isOpen)
        {
            if (ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(2000);

            EndGame();
        }
    }

    void EndGame()
    {
        if (IsServer)
        {
            foreach (var obj in FindObjectsOfType<NetworkObject>())
            {
                if (obj != NetworkManager.Singleton.GetComponent<NetworkObject>())
                {
                    obj.Despawn(true);
                }
            }
        }

        NetworkManager.Singleton.SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Single);
        //NetworkManager.Singleton.Shutdown();
    }
}
