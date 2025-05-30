using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Box : NetworkBehaviour, IDamageable
{
    const int maxHealth = 10;

    [Header("Box Network variables")]
    // Variables that need to be updated in both Clients and Server
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);
    bool isDestroyed = false;

    [Header("Box Audios")]
    [SerializeField] AudioClip boxDestroySfx;

    public override void OnNetworkSpawn()
    {

    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    void ApplyDamage(int damage)
    {
        if (currentHealth.Value <= 0 || isDestroyed) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0)
        {
            PlayBoxDestroyedClientRpc();

            if (IsServer)
            {
                float roll = Random.value;
                GameObject prefabToDrop = null;

                if (roll < 0.05f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_AssaultRifle");
                }
                else if (roll < 0.10f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_Shotgun");
                }
                else if (roll < 0.15f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_Medkit");
                }
                else if (roll < 0.20f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_AmmoBox");
                }

                if (prefabToDrop != null)
                {
                    GameObject gunToDrop = Instantiate(prefabToDrop, transform.position, transform.rotation);
                    PickupItem pickup = gunToDrop.GetComponent<PickupItem>();

                    if (pickup != null)
                    {
                        pickup.useRandomSpawnPoint = false;
                    }

                    gunToDrop.GetComponent<NetworkObject>().Spawn();
                }
            }

            NetworkObject.Despawn();
        }
    }

    // Client RPC functions
    [ClientRpc]
    public void PlayBoxDestroyedClientRpc()
    {
        SFXManager.Singleton.PlaySound(boxDestroySfx);
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        ApplyDamage(damage);
    }
}
