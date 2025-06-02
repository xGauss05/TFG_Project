using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;

    void Start()
    {
        if (ScoreManager.Singleton != null)
        {
            ScoreManager.Singleton.score.OnValueChanged += OnScoreChanged;
            UpdateScoreText(ScoreManager.Singleton.score.Value);
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Singleton != null)
        {
            ScoreManager.Singleton.score.OnValueChanged -= OnScoreChanged;
        }
    }

    void OnScoreChanged(int previousValue, int newValue)
    {
        UpdateScoreText(newValue);
    }

    void UpdateScoreText(int score)
    {
        scoreText.text = $"Score: {score}";
    }
}
