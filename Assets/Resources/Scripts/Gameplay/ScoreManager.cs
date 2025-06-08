using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Singleton { get; private set; }

    [Header("ScoreManager Network variables")]
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

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

    public void AddScore(int value)
    {
        score.Value += value;
        Debug.Log($"Current score: {score.Value}.");
    }

    public void SubstractScore(int value)
    {
        if (score.Value < value)
        {
            score.Value = 0;
        }
        else
        {
            score.Value -= value;
        }
        Debug.Log($"Current score: {score.Value}.");
    }

    public void SubmitScore()
    {
        LeaderboardManager.Singleton.UploadScore(score.Value);
    }
}
