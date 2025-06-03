using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Singleton { get; private set; }

    [Header("Timer scores")]
    [SerializeField] float baseScore = 3500.0f;
    [SerializeField] float decayRate = 0.05f;

    [Header("UI Components")]
    [SerializeField] TextMeshProUGUI timerText;

    [Header("Level Network variables")]
    public NetworkVariable<float> timer = new NetworkVariable<float>(
       0f,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server);

    // Flags for logic handling
    bool stopTimer = false;

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

        #endregion Singleton
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timer.Value = 0f;
        }
    }

    void Update()
    {
        if (IsServer && !stopTimer)
        {
            timer.Value += Time.deltaTime;
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timer.Value / 60f);
        int seconds = Mathf.FloorToInt(timer.Value % 60f);
        timerText.text = $"{minutes} : {seconds}";
    }

    public int GetTimerScore()
    {
        float score = baseScore - decayRate * timer.Value;

        return Mathf.Max(0, Mathf.RoundToInt(score));
    }

}
