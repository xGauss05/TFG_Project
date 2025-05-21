using System.Collections;
using UnityEngine;
using Unity.Netcode;

public abstract class GunBase : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] protected Transform gunMuzzle;
    [SerializeField] protected int maxCapacity = 20;
    [SerializeField] protected int currentAmmo = 20;
    [SerializeField] protected int gunDamage = 10;
    [SerializeField] protected Vector3 shotSpreadVariance = new Vector3(0.005f, 0.005f, 0.005f);
    [SerializeField] protected float fireRate = 0.0f;

    [Header("Gun Audios")]
    [SerializeField] protected AudioClip gunShotSfx;
    [SerializeField] protected AudioClip emptyClipSfx;
    [SerializeField] protected AudioClip reloadSfx;

    // Flags & variables for logic handling
    protected bool isReloading = false;
    protected float lastShotTime = 0;

    // Helpers and Components
    protected AudioSource audioSource;

    protected virtual void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

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

    protected virtual Vector3 ApplySpread(Vector3 direction)
    {
        return direction + new Vector3(
            Random.Range(-shotSpreadVariance.x, shotSpreadVariance.x),
            Random.Range(-shotSpreadVariance.y, shotSpreadVariance.y),
            Random.Range(-shotSpreadVariance.z, shotSpreadVariance.z)
        );
    }

    public abstract void Shoot(Vector3 origin, Vector3 direction);

    public virtual void Reload()
    {
        if (isReloading || currentAmmo >= maxCapacity) return;
        StartCoroutine(ReloadCoroutine());
    }

    protected virtual IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        audioSource?.PlayOneShot(reloadSfx);
        yield return new WaitForSeconds(reloadSfx.length);
        currentAmmo = maxCapacity;
        isReloading = false;
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    protected void SpawnTrailClientRpc(Vector3 start, Vector3 end)
    {
        GameObject trail = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/BulletTrail"));
        trail.GetComponent<BulletTrail>()?.SetTrailPositions(start, end);
        trail.GetComponent<NetworkObject>().Spawn(true);
    }

}
