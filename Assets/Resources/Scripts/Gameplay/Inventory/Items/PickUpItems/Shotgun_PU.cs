using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun_PU : PickupItem
{
    const int score = 15;

    public override void OnPickup(Player player)
    {
        player.inventory.AddGun(player.GetGunBaseComponent(GunBase.Type.Shotgun));
        //Debug.Log($"{player.steamName.Value} obtained a Shotgun.");
        if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);
    }
}
