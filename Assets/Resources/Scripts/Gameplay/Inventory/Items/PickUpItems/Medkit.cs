using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medkit : PickupItem
{
    const int score = 10;

    public override void OnPickup(Player player)
    {
        player.inventory.AddMedkit();
        //Debug.Log($"{player.steamName.Value} obtained a Medkit.");
        if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);
    }
}
