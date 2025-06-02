using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : PickupItem
{
    [SerializeField] int ammoAmount = 10;
    const int score = 10;

    public override void OnPickup(Player player)
    {
        player.inventory.AddAmmo(ammoAmount);
        //Debug.Log($"{player.steamName.Value} obtained {ammoAmount} ammo.");
        if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);
    }
}
