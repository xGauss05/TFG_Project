using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssaultRifle_PU : PickupItem
{
    public override void OnPickup(Player player)
    {
        var newRifle = new AssaultRifle();
        player.inventory.AddGun(newRifle);
        Debug.Log($"{player.steamName.Value} obtained an Assault Rifle.");
    }
}
