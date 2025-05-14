using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medkit : PickupItem
{
    [SerializeField] int quantity = 1;

    public override void OnPickup(Player player)
    {
        //player.AddMedkit(quantity); // NYI
    }
}
