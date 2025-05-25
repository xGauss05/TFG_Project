using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Singleton { get; private set; }
    AudioSource sfx_source;

    void Awake()
    {
        sfx_source = GetComponent<AudioSource>();

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

    public void PlaySound(AudioClip sound)
    {
        sfx_source.PlayOneShot(sound);
    }
}
