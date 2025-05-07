using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;

    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    const string master = "MasterVolume";
    const string bgm = "BGMVolume";
    const string sfx = "SFXVolume";

    void Awake()
    {
        masterSlider.value = PlayerPrefs.HasKey(master) ? PlayerPrefs.GetFloat(master) : GetMixerLinearValue("Master");
        bgmSlider.value = PlayerPrefs.HasKey(bgm) ? PlayerPrefs.GetFloat(bgm) : GetMixerLinearValue("BGM");
        sfxSlider.value = PlayerPrefs.HasKey(sfx) ? PlayerPrefs.GetFloat(sfx) : GetMixerLinearValue("SFX");

        SetMasterVolume(masterSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    float GetMixerLinearValue(string exposedParam)
    {
        float dB;
        if (audioMixer.GetFloat(exposedParam, out dB))
        {
            return Mathf.Pow(10f, dB / 20f);
        }
        return 1f;
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(value, "Master", master);
    }

    public void SetBGMVolume(float value)
    {
        SetVolume(value, "BGM", bgm);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(value, "SFX", sfx);
    }

    void SetVolume(float value, string mixerParam, string playerPrefKey)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(mixerParam, dB);
        PlayerPrefs.SetFloat(playerPrefKey, value);
    }
}
