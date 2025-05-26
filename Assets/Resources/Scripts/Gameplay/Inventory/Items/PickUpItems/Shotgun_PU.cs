using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun_PU : PickupItem
{
    public override void OnPickup(Player player)
    {
        var newShotgun = new Shotgun();
        player.inventory.AddGun(newShotgun);
        Debug.Log($"{player.steamName.Value} obtained a Shotgun.");
    }
}
