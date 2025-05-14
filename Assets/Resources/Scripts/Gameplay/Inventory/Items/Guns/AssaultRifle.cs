using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssaultRifle : GunBase
{
    [SerializeField] float fireRate = 0.1f;
    float lastShotTime = 0;

    public override void Shoot(Vector3 origin, Vector3 direction)
    {
        if (isReloading || Time.time - lastShotTime < fireRate) return;

        if (currentAmmo <= 0)
        {
            audioSource?.PlayOneShot(emptyClipSfx);
            return;
        }

        audioSource?.PlayOneShot(gunShotSfx);
        currentAmmo--;

        GameObject trail = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/BulletTrail"));

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 999.0f))
        {
            trail.GetComponent<BulletTrail>()?.SetTrailPositions(origin, hit.point);
            hit.collider.GetComponent<BasicZombie>()?.TakeDamageServerRpc(gunDamage);
        }
        else
        {
            trail.GetComponent<BulletTrail>()?.SetTrailPositions(origin, origin + direction * 999f);
        }

        lastShotTime = Time.time;
    }
}
