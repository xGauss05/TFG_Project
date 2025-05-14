using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class PickupItem : NetworkBehaviour
{
    public abstract void OnPickup(Player player);

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                //player.PickupItem(this); // NYI
                Despawn();
                Debug.Log("Item pickup!");
            }
        }
    }

    void Despawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}