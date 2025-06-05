using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Car : NetworkBehaviour, IDamageable
{
    const int maxHealth = 1000;
    const int score = 200;

    [Header("Car Network variables")]
    // Variables that need to be updated in both Clients and Server
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);
    public bool isAlarmed = false;
    bool isDestroyed = false;

    [Header("Car Audios")]
    [SerializeField] AudioClip carAlarmSfx;
    [SerializeField] AudioClip carHitSfx;
    [SerializeField] AudioClip carExplosionSfx;

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
        PlayCarHitClientRpc();

        if (isAlarmed)
        {
            isAlarmed = !isAlarmed;
            PlayCarAlarmClientRpc();
            if (HordeManager.Singleton != null)
            {
                HordeManager.Singleton.StartHorde();
            }
        }

        if (currentHealth.Value <= 0)
        {
            PlayCarExplosionClientRpc();

            if (HordeManager.Singleton != null)
            {
                HordeManager.Singleton.StartHorde();
            }

            if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.AddScore(score);

            NetworkObject.Despawn();
        }
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    public void PlayCarAlarmClientRpc()
    {
        SFXManager.Singleton.PlaySound(carAlarmSfx);
    }

    [ClientRpc]
    public void PlayCarExplosionClientRpc()
    {
        SFXManager.Singleton.PlaySound(carExplosionSfx);
    }

    [ClientRpc]
    public void PlayCarHitClientRpc()
    {
        SFXManager.Singleton.PlaySound(carHitSfx);
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        ApplyDamage(damage);
    }
}
