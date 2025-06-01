using Steamworks.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingMenu : MonoBehaviour
{
    [SerializeField] GameObject RankingObject;
    List<GameObject> rankers = new List<GameObject>();

    [Header("Prefabs")]
    [SerializeField] GameObject rankerInfoPrefab;

    void OnEnable()
    {
        UpdateRankingList();
    }

    async void UpdateRankingList()
    {
        foreach (var ranker in rankers)
        {
            Destroy(ranker);
        }
        rankers.Clear();

        LeaderboardEntry[] lb = await LeaderboardManager.Singleton.DownloadTopScores();

        foreach (var entry in lb)
        {
            GameObject rankerItem = Instantiate(rankerInfoPrefab, RankingObject.transform);
            RankerInfoUI rankerInfo = rankerItem.GetComponent<RankerInfoUI>();

            rankerInfo.rank.text = $"#{entry.GlobalRank}";
            rankerInfo.playerName.text = $"{entry.User.Name}";
            rankerInfo.score.text = $"{entry.Score}";

            Steamworks.Data.Image? image = await entry.User.GetLargeAvatarAsync();
            if (image != null)
            {
                Texture2D tex2d = new Texture2D((int)image.Value.Width, (int)image.Value.Height, TextureFormat.RGBA32, false);
                tex2d.LoadRawTextureData(image.Value.Data);
                tex2d.Apply();

                rankerInfo.rankerImage.texture = tex2d;
            }

            rankers.Add(rankerItem);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(RankingObject.GetComponent<RectTransform>());
    }
}
