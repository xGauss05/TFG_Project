using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssaultRifle_PU : PickupItem
{
    const int score = 15;

    public override void OnPickup(Player player)
    {
        player.inventory.AddGun(player.GetGunBaseComponent(GunBase.Type.AssaultRifle));
        //Debug.Log($"{player.steamName.Value} obtained an Assault Rifle.");
        if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);
    }
}
