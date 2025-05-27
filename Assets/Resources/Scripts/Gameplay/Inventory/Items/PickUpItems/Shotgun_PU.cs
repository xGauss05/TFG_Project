using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun_PU : PickupItem
{
    public override void OnPickup(Player player)
    {
        player.inventory.AddGun(player.GetGunBaseComponent(GunBase.Type.Shotgun));
        Debug.Log($"{player.steamName.Value} obtained a Shotgun.");
    }
}
