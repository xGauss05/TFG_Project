using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medkit : PickupItem
{
    public override void OnPickup(Player player)
    {
        player.inventory.AddMedkit();
        Debug.Log($"{player.steamName} obtained a Medkit.");
    }
}
