using System.Collections;
using System.Collections.Generic;
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

        BGMManager.Singleton.ChangeBGM(0);
    }

    void Update()
    {

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentTrackIndex++;

            if (currentTrackIndex >= musicTracksSize) currentTrackIndex = 0;

            ChangeBGM(musicTracks[currentTrackIndex]);
        }
#endif

    }

    public void ChangeBGM(AudioClip musicToChange)
    {
        currentBgm.Stop();
        currentBgm.clip = musicToChange;
        currentBgm.Play();
    }

    public void ChangeBGM(int audioIndex)
    {
        currentTrackIndex = audioIndex;
        currentBgm.Stop();

        currentBgm.clip = musicTracks[audioIndex];
        currentBgm.Play();
    }

}