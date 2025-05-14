using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : PickupItem
{
    [SerializeField] int ammoAmount = 10;

    public override void OnPickup(Player player)
    {
        //player.AddAmmo(ammoAmount); // NYI        
    }
}
