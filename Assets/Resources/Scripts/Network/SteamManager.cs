using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using TMPro;
using Steamworks.Data;
using System;

public class SteamManager : MonoBehaviour
{
    [SerializeField] int maxPlayers;
    [SerializeField] TMP_InputField LobbyIDInputField;

    [SerializeField] TextMeshProUGUI LobbyIDText;

    [SerializeField] GameObject MainMenu;
    [SerializeField] GameObject InLobbyMenu;
    [SerializeField] GameObject LobbyChat;

    [SerializeField] TextMeshProUGUI PlayerFeedback;

    void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
    }

    void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
    }

    void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);

            Debug.Log($"Created lobby {lobby.Id}");
        }
    }

    void LobbyEntered(Lobby lobby)
    {
        LobbyReference.Singleton.currentLobby = lobby;
        LobbyIDText.text = lobby.Id.ToString();

        MainMenu.SetActive(false);
        InLobbyMenu.SetActive(true);
        LobbyChat.SetActive(true);

        Debug.Log($"Entered lobby {lobby.Id}");
    }

    async void GameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        await lobby.Join();
    }

    public async void HostLobby()
    {
        await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
    }

    public async void JoinLobbyWithID()
    {
        ulong ID;

        // Steam lobby IDs are 'ulong' type of variable
        if (!ulong.TryParse(LobbyIDInputField.text, out ID))
        {
            Debug.Log($"{LobbyIDInputField.text} is not a valid Lobby ID.");
            PlayerFeedback.text = $"{LobbyIDInputField.text} is not a valid Lobby ID.";
            return;
        }

        // Asks for the lobbies with at least 1 spot available (1)
        Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        foreach (Lobby l in lobbies)
        {
            if (l.Id == ID)
            {
                await l.Join();
                return;
            }
        }
    }

    public void CopyID()
    {
        TextEditor tEditor = new TextEditor();
        tEditor.text = LobbyIDText.text;
        tEditor.SelectAll();
        tEditor.Copy();

        Debug.Log($"Successfully copied lobby ID: {LobbyIDText.text}.");
        PlayerFeedback.text = $"Successfully copied lobby ID: {LobbyIDText.text}.";
    }

    public void LeaveLobby()
    {
        LobbyReference.Singleton.currentLobby?.Leave();
        LobbyReference.Singleton.currentLobby = null;

        MainMenu.SetActive(true);
        InLobbyMenu.SetActive(false);
        LobbyChat.SetActive(false);

        if (PlayerFeedback.text.Length > 0) PlayerFeedback.text = "";
    }
}
