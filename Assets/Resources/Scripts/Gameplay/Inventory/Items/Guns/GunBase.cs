using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] protected Transform gunMuzzle;
    [SerializeField] protected int maxCapacity = 20;
    [SerializeField] protected int currentAmmo = 20;
    [SerializeField] protected int gunDamage = 10;
    [SerializeField] protected Vector3 shotSpreadVariance = new Vector3(0.005f, 0.005f, 0.005f);
    protected bool isReloading = false;

    [Header("Gun Audios")]
    [SerializeField] protected AudioClip gunShotSfx;
    [SerializeField] protected AudioClip emptyClipSfx;
    [SerializeField] protected AudioClip reloadSfx;

    // Helpers and Components
    protected AudioSource audioSource;

    protected virtual void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public virtual (Vector3 origin, Vector3 direction) CalculateShot()
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

    public virtual void Shoot(Vector3 origin, Vector3 direction)
    {
        if (isReloading) return;

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
    }

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
}
