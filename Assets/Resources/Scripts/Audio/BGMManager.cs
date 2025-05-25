using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Singleton { get; private set; }

    public AudioSource currentBgm;
    [SerializeField] AudioClip[] musicTracks;

    [Header("Do not modify these parameters")]
    [SerializeField, HideInInspector] int musicTracksSize = 0;
    [SerializeField, HideInInspector] int currentTrackIndex = 0;

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

    void Start()
    {
        foreach (AudioClip ac in musicTracks)
        {
            musicTracksSize++;
        }

        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
        {
            ChangeBGM(0);
        }
    }

    public void ChangeBGM(int audioIndex)
    {
        if (audioIndex < 0 || audioIndex >= musicTracks.Length) return; // Safety measure

        currentTrackIndex = audioIndex;
        currentBgm.Stop();

        currentBgm.clip = musicTracks[audioIndex];
        currentBgm.Play();
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void ChangeBGMClientRpc(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= musicTracks.Length) return;

        currentTrackIndex = trackIndex;
        currentBgm.Stop();
        currentBgm.clip = musicTracks[trackIndex];
        currentBgm.Play();
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc(RequireOwnership = false)]
    public void RequestChangeBGMServerRpc(int trackIndex)
    {
        ChangeBGMClientRpc(trackIndex);
    }
}