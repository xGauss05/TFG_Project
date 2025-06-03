using Steamworks;
using Steamworks.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Singleton { get; private set; }

    string gameRanking = "HighScoreInLightsOutTFG";

    Leaderboard leaderboard;

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
        RequestLeaderboard();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            UploadScore(0);
        }
    }

    async void RequestLeaderboard()
    {
        Leaderboard? lb = await SteamUserStats.FindOrCreateLeaderboardAsync(gameRanking,
            LeaderboardSort.Descending,
            LeaderboardDisplay.Numeric);

        if (lb != null)
        {
            leaderboard = (Leaderboard)lb;
            //Debug.Log("Leaderboard loaded: " + lb.Value.Name);
        }
    }

    async public void UploadScore(int score)
    {
        await leaderboard.SubmitScoreAsync(score);
    }

    public async Task<LeaderboardEntry[]> DownloadTopScores()
    {
        LeaderboardEntry[] entries = await leaderboard.GetScoresAsync(10);

        //foreach (var entry in entries)
        //{
        //    Debug.Log($"{entry.GlobalRank}: {entry.User.Name} - {entry.Score}");
        //}

        return entries;
    }
}
