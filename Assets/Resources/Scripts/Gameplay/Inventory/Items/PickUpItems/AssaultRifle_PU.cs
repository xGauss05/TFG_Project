using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssaultRifle_PU : PickupItem
{
    public override void OnPickup(Player player)
    {
        player.inventory.AddGun(player.GetGunBaseComponent(GunBase.Type.AssaultRifle));
        Debug.Log($"{player.steamName.Value} obtained an Assault Rifle.");
    }
}
