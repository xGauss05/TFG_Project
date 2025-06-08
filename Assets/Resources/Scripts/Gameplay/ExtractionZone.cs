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
    [Header("Timer scores")]
    [SerializeField] int score = 2000;

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

    void OnTriggerStay(Collider other)
    {
        if (IsClient) return;

        if (!other.CompareTag("Player")) { return; }

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
        Player[] players = FindObjectsOfType<Player>();
        int deadPlayers = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].isDead != null && players[i].isDead.Value)
            {
                deadPlayers++;
            }
        }

        if (playersInside >= (requiredPlayers - deadPlayers) && !safeDoor.isOpen)
        {
            if (ScoreManager.Singleton != null)
            {
                // Level end score
                int scoreToUpload = score - (score * deadPlayers / requiredPlayers);

                // Timer based score
                if (LevelManager.Singleton != null)
                {
                    scoreToUpload += LevelManager.Singleton.GetTimerScore();
                    //Debug.Log("Added timer score.");
                }

                ScoreManager.Singleton.AddScore(scoreToUpload);
            }

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

        ScoreManager.Singleton.SubmitScore();
        NetworkManager.Singleton.SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Single);
    }
}
