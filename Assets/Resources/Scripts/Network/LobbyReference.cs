using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data;
using UnityEngine.Events;
using Unity.Netcode;

public class LobbyReference : MonoBehaviour
{
    public static LobbyReference Singleton { get; private set; }

    public Lobby? currentLobby;

    void Awake()
    {
        #region Singleton

        if (Singleton != null && Singleton != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Singleton = this;
        }

        DontDestroyOnLoad(this.gameObject);

        #endregion Singleton
    }

}
