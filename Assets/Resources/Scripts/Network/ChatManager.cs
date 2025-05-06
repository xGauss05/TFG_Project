using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;
using Steamworks.Data;
using System;

public class ChatManager : MonoBehaviour
{
    [SerializeField] TMP_InputField ChatInputField;
    [SerializeField] GameObject ChatGameObject;

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
        ChatGameObject.GetComponent<TextMeshProUGUI>().text = "";
    }

    void LobbyMemberLeave(Lobby lobby, Friend friend)
    {
        ChatGameObject.GetComponent<TextMeshProUGUI>().text += friend.Name + " left the lobby.\n";
    }

    void LobbyMemberJoined(Lobby lobby, Friend friend)
    {
        ChatGameObject.GetComponent<TextMeshProUGUI>().text += friend.Name + " joined the lobby.\n";
    }

    void LobbyEntered(Lobby lobby)
    {
        ChatGameObject.GetComponent<TextMeshProUGUI>().text += "You entered the lobby.\n";
    }

    void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        ChatGameObject.GetComponent<TextMeshProUGUI>().text += friend.Name + ": " + msg + "\n";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendMessage();
        }
    }

    void SendMessage()
    {
        if (!string.IsNullOrEmpty(ChatInputField.text))
        {
            LobbyReference.Singleton.currentLobby?.SendChatString(ChatInputField.text);
            ChatInputField.text = "";
        }
    }
}
