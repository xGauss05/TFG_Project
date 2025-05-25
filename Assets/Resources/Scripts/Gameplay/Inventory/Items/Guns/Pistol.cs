using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : GunBase
{
    public override void Shoot(Vector3 origin, Vector3 direction)
    {
        if (isReloading || Time.time - lastShotTime < fireRate) return;

        if (currentAmmo <= 0 || Time.time - lastShotTime < fireRate)
        {
            PlayEmptyClipSFXClientRpc();
            lastShotTime = Time.time;
            return;
        }

        PlayGunShotSFXClientRpc();
        currentAmmo--;

        Vector3 hitPoint = origin + direction * 999f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 999.0f))
        {
            hitPoint = hit.point;
            hit.collider.GetComponent<BasicZombie>()?.TakeDamageServerRpc(gunDamage);
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
