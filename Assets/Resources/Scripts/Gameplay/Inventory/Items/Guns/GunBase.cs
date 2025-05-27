using System.Collections;
using UnityEngine;
using Unity.Netcode;

public abstract class GunBase : NetworkBehaviour
{
    public enum Type
    {
        None,
        Pistol,
        AssaultRifle,
        Shotgun
    }

    [Header("Gun Settings")]
    [SerializeField] protected Transform gunMuzzle;
    protected virtual int maxCapacity => 8;
    [SerializeField] public NetworkVariable<int> currentAmmo;
    [SerializeField] protected int gunDamage = 10;
    [SerializeField] protected Vector3 shotSpreadVariance = new Vector3(0.005f, 0.005f, 0.005f);
    [SerializeField] protected float fireRate = 0.0f;
    public Type GunType { get; protected set; } = Type.None;

    [Header("Gun Audios")]
    [SerializeField] protected AudioClip gunShotSfx;
    [SerializeField] protected AudioClip emptyClipSfx;
    [SerializeField] protected AudioClip reloadSfx;

    // Flags & variables for logic handling
    public bool isReloading { get; private set; } = false;
    protected float lastShotTime = 0;

    public (Vector3 origin, Vector3 direction) CalculateShot()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        Plane gunPlane = new Plane(gunMuzzle.forward, gunMuzzle.position);
        Vector3 bulletDirection = gunMuzzle.forward;

        if (Physics.Raycast(ray, out RaycastHit reticleTarget, 999.0f) &&
            gunPlane.Raycast(ray, out float distanceToPlane))
        {
            if (distanceToPlane < reticleTarget.distance)
            {
                bulletDirection = (reticleTarget.point - gunMuzzle.position).normalized;
            }
            else if (Physics.Raycast(reticleTarget.point, ray.direction, out RaycastHit targetPastReticle, 999f))
            {
                bulletDirection = (targetPastReticle.point - gunMuzzle.position).normalized;
            }
            else
            {
                bulletDirection = ray.direction;
            }
        }
        else
        {
            bulletDirection = ray.direction;
        }

        bulletDirection = ApplySpread(bulletDirection);
        return (gunMuzzle.position, bulletDirection);
    }

    protected Vector3 ApplySpread(Vector3 direction)
    {
        return direction + new Vector3(
            Random.Range(-shotSpreadVariance.x, shotSpreadVariance.x),
            Random.Range(-shotSpreadVariance.y, shotSpreadVariance.y),
            Random.Range(-shotSpreadVariance.z, shotSpreadVariance.z)
        );
    }

    public abstract void Shoot(Vector3 origin, Vector3 direction);

    public void Reload()
    {
        if (isReloading || currentAmmo.Value >= maxCapacity) return;

        StartCoroutine(ReloadCoroutine());
    }

    protected IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        PlayReloadSFXClientRpc();

        yield return new WaitForSeconds(reloadSfx.length);

        currentAmmo.Value = maxCapacity;
        isReloading = false;
    }

    protected void SpawnTrail(Vector3 start, Vector3 end)
    {
        GameObject trail = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Items/Guns/BulletTrail"));
        trail.GetComponent<BulletTrail>()?.SetTrailPositions(start, end);
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    protected void SpawnTrailClientRpc(Vector3 origin, Vector3 hitPoint)
    {
        SpawnTrail(origin, hitPoint);
    }

    [ClientRpc]
    protected void PlayGunShotSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(gunShotSfx);
    }

    [ClientRpc]
    protected void PlayReloadSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(reloadSfx);
    }

    [ClientRpc]
    protected void PlayEmptyClipSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(emptyClipSfx);
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    protected void SpawnTrailServerRpc(Vector3 start, Vector3 end)
    {
        SpawnTrailClientRpc(start, end);
    }

}
