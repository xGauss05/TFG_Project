using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Box : NetworkBehaviour, IDamageable
{
    const int maxHealth = 10;
    const int score = 5;

    [Header("Box Network variables")]
    // Variables that need to be updated in both Clients and Server
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);
    bool isDestroyed = false;

    [Header("Box Audios")]
    [SerializeField] AudioClip boxDestroySfx;

    [Header("Box Particles")]
    [SerializeField] GameObject ps_boxDestroyed;

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
            SpawnBoxEffectClientRpc();

            if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);

            if (IsServer)
            {
                float roll = Random.value;
                GameObject prefabToDrop = null;

                if (roll < 0.10f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_AssaultRifle");
                }
                else if (roll < 0.20f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_Shotgun");
                }
                else if (roll < 0.40f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_Medkit");
                }
                else if (roll < 0.60f)
                {
                    prefabToDrop = Resources.Load<GameObject>("Prefabs/Gameplay/Items/PickUpItems/PickUp_AmmoBox");
                }

                if (prefabToDrop != null)
                {
                    GameObject itemToDrop = Instantiate(prefabToDrop, transform.position, transform.rotation);
                    itemToDrop.GetComponent<NetworkObject>().Spawn();
                }
            }

            NetworkObject.Despawn();
        }
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    public void PlayBoxDestroyedClientRpc()
    {
        SFXManager.Singleton.PlaySound(boxDestroySfx);
    }

    [ClientRpc]
    void SpawnBoxEffectClientRpc()
    {
        if (ps_boxDestroyed == null) return;

        GameObject box_particle = Instantiate(ps_boxDestroyed, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Destroy(box_particle, 0.5f);
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        ApplyDamage(damage);
    }
}
