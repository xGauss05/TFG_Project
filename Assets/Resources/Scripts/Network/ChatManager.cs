using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviour
{
    [Header("Chat UI properties")]
    [SerializeField] TMP_InputField ChatInputField;
    [SerializeField] TextMeshProUGUI ChatText;
    [SerializeField] ScrollRect ChatScrollRect;

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
        ChatText.text = "";
    }

    void OnDisable()
    {
        SteamMatchmaking.OnChatMessage -= ChatSent;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeave;
        ChatText.text = "";
    }

    void LobbyMemberLeave(Lobby lobby, Friend friend)
    {
        AppendMessage(friend.Name + " left the lobby.");
    }

    void LobbyMemberJoined(Lobby lobby, Friend friend)
    {
        AppendMessage(friend.Name + " joined the lobby.");
    }

    void LobbyEntered(Lobby lobby)
    {
        AppendMessage("You entered the lobby.");
    }

    void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        AppendMessage(friend.Name + ": " + msg);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ChatInputField.ActivateInputField();
            SendMessage();
        }
    }

    void SendMessage()
    {
        if (!string.IsNullOrEmpty(ChatInputField.text))
        {
            LobbyReference.Singleton.currentLobby?.SendChatString(ChatInputField.text);
            ChatInputField.text = "";
            ChatInputField.DeactivateInputField();
        }
    }

    void AppendMessage(string message)
    {
        ChatText.text += message + "\n";

        Canvas.ForceUpdateCanvases();
        ChatScrollRect.verticalNormalizedPosition = 0f;
    }
}
