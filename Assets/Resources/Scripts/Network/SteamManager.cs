using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using TMPro;
using Steamworks.Data;
using System;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SteamManager : MonoBehaviour
{
    [SerializeField] int maxPlayers;

    [Header("Main Menu")]
    [SerializeField] GameObject MainMenu;
    [SerializeField] TextMeshProUGUI PlayerFeedback;

    [Header("Lobby Menu")]
    [SerializeField] GameObject LobbyScreen;
    [SerializeField] GameObject LobbyIDScreen;
    [SerializeField] TextMeshProUGUI LobbyIDText;
    [SerializeField] GameObject LobbyChat;
    [SerializeField] GameObject LobbyPlayersList;
    List<GameObject> currentPlayers = new List<GameObject>();
    [SerializeField] GameObject StartGameButton;

    [Header("Join Lobby Menu")]
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] TMP_InputField LobbyIDInputField;

    [Header("Prefabs")]
    [SerializeField] GameObject playerInfoPrefab;

    void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
    }

    void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
        SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeave;
    }

    void Start()
    {
        if (LobbyReference.Singleton.currentLobby != null)
        {
            LobbyIDText.text = LobbyReference.Singleton.currentLobby.Value.Id.ToString();

            MainMenu.SetActive(false);
            LobbyScreen.SetActive(true);
            LobbyChat.SetActive(true);
            LobbyIDScreen.SetActive(true);
            LobbyPlayersList.SetActive(true);

            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId =
                    LobbyReference.Singleton.currentLobby.Value.Owner.Id;

                NetworkManager.Singleton.StartClient();
            }

            UpdatePlayerListUI();
        }
    }

    void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);

            NetworkManager.Singleton.StartHost();
            StartGameButton.SetActive(true);
            LobbyReference.Singleton.currentLobby = lobby;

            UpdatePlayerListUI();
            Debug.Log($"Created lobby {lobby.Id}");
        }
    }

    void LobbyEntered(Lobby lobby)
    {
        LobbyReference.Singleton.currentLobby = lobby;
        LobbyIDText.text = lobby.Id.ToString();

        MainMenu.SetActive(false);
        JoinLobbyScreen.SetActive(false);
        LobbyScreen.SetActive(true);
        LobbyChat.SetActive(true);
        LobbyIDScreen.SetActive(true);
        LobbyPlayersList.SetActive(true);

        Debug.Log($"Entered lobby {lobby.Id}");

        if (NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
        StartGameButton.SetActive(false);

        UpdatePlayerListUI();

    }

    async void GameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        await lobby.Join();
    }

    void LobbyMemberLeave(Lobby lobby, Friend friend)
    {
        bool ownerStillInLobby = false;

        foreach (var member in lobby.Members)
        {
            if (member.Id == lobby.Owner.Id)
            {
                ownerStillInLobby = true;
                break;
            }
        }

        if (!ownerStillInLobby)
        {
            LeaveLobby();
            PlayerFeedback.text = "The host has disconnected.";
            return;
        }

        UpdatePlayerListUI();
    }

    void LobbyMemberJoined(Lobby lobby, Friend friend)
    {
        UpdatePlayerListUI();
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

        try
        {
            Lobby l = new Lobby(ID);
            await l.Join();
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to join lobby: {ID}, {ex.Message}");
            PlayerFeedback.text = $"Failed to join lobby: {ID}, {ex.Message}";
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

        NetworkManager.Singleton.Shutdown();

        MainMenu.SetActive(true);
        LobbyScreen.SetActive(false);
        LobbyChat.SetActive(false);
        LobbyIDScreen.SetActive(false);
        StartGameButton.SetActive(false);
        LobbyPlayersList.SetActive(false);
    }

    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("2_Gameplay", LoadSceneMode.Single);
        }
    }

    async void UpdatePlayerListUI()
    {
        foreach (var player in currentPlayers)
        {
            Destroy(player);
        }
        currentPlayers.Clear();

        foreach (var player in LobbyReference.Singleton.currentLobby?.Members)
        {
            GameObject playerItem = Instantiate(playerInfoPrefab, LobbyPlayersList.transform);
            PlayerInfoUI playerInfo = playerItem.GetComponentInChildren<PlayerInfoUI>();

            playerInfo.playerName.text = player.Name;

            Steamworks.Data.Image? image = await player.GetLargeAvatarAsync();

            if (image != null)
            {
                Texture2D tex2d = new Texture2D((int)image.Value.Width, (int)image.Value.Height, TextureFormat.RGBA32, false);
                tex2d.LoadRawTextureData(image.Value.Data);
                tex2d.Apply();

                playerInfo.playerImage.texture = tex2d;
            }

            currentPlayers.Add(playerItem);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(LobbyPlayersList.GetComponent<RectTransform>());
    }

}