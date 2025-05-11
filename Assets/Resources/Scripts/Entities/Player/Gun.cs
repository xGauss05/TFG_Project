using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] Transform gunMuzzle;
    [SerializeField] const int gunDamage = 10;
    [SerializeField] int currentAmmo = 20;
    [SerializeField] const int maxCapacity = 20;
    [SerializeField] Vector3 shotSpreadVariance = new Vector3(0.005f, 0.005f, 0.005f);

    Vector3 lastDebugOrigin;
    Vector3 debugHit = Vector3.zero;
    Vector3 reticleDebugHit = Vector3.zero;
    Vector3 furtherReticleDebugHit = Vector3.zero;

    AudioSource audioSource;
    [SerializeField] AudioClip gunShotSfx;
    [SerializeField] AudioClip emptyClipSfx;
    [SerializeField] AudioClip reloadSfx;

    bool isReloading = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public (Vector3 origin, Vector3 direction) CalculateShot()
    {
        // Create the ray forward from the camera
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        // Create the gun plane
        Plane gunPlane = new Plane(gunMuzzle.forward, gunMuzzle.position);

        // Declare the final direction of the bullet, to be overwritten later
        Vector3 bulletDirection = gunMuzzle.forward;

        // Shoot rays FROM CAMERA for the target and for the plane
        if (Physics.Raycast(ray, out RaycastHit reticleTarget, 999.0f) &&
            gunPlane.Raycast(ray, out float distanceToPlane))
        {
            reticleDebugHit = reticleTarget.point;

            // Check if the hit object is further that the hit on the gun plane
            if (distanceToPlane < reticleTarget.distance)
            {
                Debug.Log("Shooting to reticle");
                bulletDirection = (reticleTarget.point - gunMuzzle.position).normalized;
            }
            else
            {
                // Shoot the same ray but with the previously hit point as origin
                if (Physics.Raycast(reticleTarget.point, ray.direction, out RaycastHit targetPastReticle, 999f))
                {
                    furtherReticleDebugHit = targetPastReticle.point;

                    Debug.Log("Shooting past reticle");
                    bulletDirection = (targetPastReticle.point - gunMuzzle.position).normalized;
                }
                else
                {
                    bulletDirection = ray.direction;
                    Debug.Log("Aiming at infinite");
                }
            }
        }
        else
        {
            bulletDirection = ray.direction;
            Debug.Log("Aiming at infinite");
        }

        bulletDirection = ShootSpread(bulletDirection);

        return (gunMuzzle.position, bulletDirection);
    }

    public void Shoot(Vector3 origin, Vector3 direction)
    {
        if (isReloading) return;

        if (currentAmmo <= 0)
        {
            audioSource.PlayOneShot(emptyClipSfx);
            return;
        }

        audioSource.PlayOneShot(gunShotSfx);
        currentAmmo--;

        GameObject trail = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/BulletTrail"));

        if (Physics.Raycast(origin, direction, out RaycastHit bulletHit, 999.0f))
        {
            lastDebugOrigin = gunMuzzle.position;

            // This is where the bullet must land
            debugHit = bulletHit.point;
            //Debug.Log($"Bullet hit {bulletHit.collider.gameObject.name}");

            trail.GetComponent<BulletTrail>().SetTrailPositions(gunMuzzle.position, bulletHit.point);

            bulletHit.collider.GetComponent<BasicZombie>()?.TakeDamageServerRpc(gunDamage);
        }
        else
        {
            Debug.Log("Bullet hit nothing.");
            trail.GetComponent<BulletTrail>().SetTrailPositions(gunMuzzle.position, gunMuzzle.forward * 999f);
        }
    }

    Vector3 ShootSpread(Vector3 direction)
    {
        return direction += new Vector3(
            Random.Range(-shotSpreadVariance.x, shotSpreadVariance.x),
            Random.Range(-shotSpreadVariance.y, shotSpreadVariance.y),
            Random.Range(-shotSpreadVariance.z, shotSpreadVariance.z)
        );
    }

    public void Reload()
    {
        if (isReloading || currentAmmo >= maxCapacity) return;
        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        audioSource.PlayOneShot(reloadSfx);
        yield return new WaitForSeconds(reloadSfx.length);

        currentAmmo = maxCapacity;
        isReloading = false;
    }

    void OnDrawGizmos()
    {
        // Green is muzzle to hitPoint
        Debug.DrawLine(lastDebugOrigin, debugHit, Color.green);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(reticleDebugHit, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(furtherReticleDebugHit, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(debugHit, 0.05f);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(gunMuzzle.position, gunMuzzle.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(5f, 5f, 0.0001f));
    }
}