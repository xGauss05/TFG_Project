using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AssaultRifle : GunBase
{
    protected override int maxCapacity => 50;

    void Awake()
    {
        GunType = Type.AssaultRifle;
    }

    public override void Shoot(Vector3 origin, Vector3 direction)
    {
        if (isReloading || Time.time - lastShotTime < fireRate) return;

        if (currentAmmo.Value <= 0 || Time.time - lastShotTime < fireRate)
        {
            PlayEmptyClipSFXClientRpc();
            lastShotTime = Time.time;
            return;
        }

        PlayGunShotSFXClientRpc();
        currentAmmo.Value--;

        Vector3 hitPoint = origin + direction * 999f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 999.0f))
        {
            hitPoint = hit.point;
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(gunDamage);
            }
        }

        if (IsServer)
        {
            SpawnTrailClientRpc(origin, hitPoint);
        }
        else
        {
            SpawnTrailServerRpc(origin, hitPoint);
        }

        lastShotTime = Time.time;
    }
}
