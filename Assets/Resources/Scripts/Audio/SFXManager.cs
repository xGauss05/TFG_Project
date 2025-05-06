using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Singleton { get; private set; }
    private AudioSource sfx_source;

    private void Awake()
    {
        sfx_source = GetComponent<AudioSource>();
        sfx_source.volume = 50;

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
