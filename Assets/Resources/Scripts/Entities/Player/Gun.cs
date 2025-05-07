using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] Transform gunMuzzle;
    [SerializeField] int gunDamage = 20;

    Vector3 lastDebugOrigin;
    Vector3 debugHit = Vector3.zero;
    Vector3 reticleDebugHit = Vector3.zero;
    Vector3 furtherReticleDebugHit = Vector3.zero;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    //public PlayerAction CalculateShot()
    //{
    //    //Create the ray forward from the camera
    //    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
    //    Ray ray = Camera.main.ScreenPointToRay(screenCenter);

    //    //Create the gun plane
    //    Plane gunPlane = new Plane(gunMuzzle.forward, gunMuzzle.position);

    //    Vector3 bulletDirection = gunMuzzle.forward; //declare the final directoin of the bullet, to be overwritten later

    //    //Shoot rays FROM CAMERA for the target and for the plane
    //    if (Physics.Raycast(ray, out RaycastHit reticleTarget, 999f) &&
    //        gunPlane.Raycast(ray, out float distanceToPlane))
    //    {
    //        reticleDebugHit = reticleTarget.point;

    //        if (distanceToPlane < reticleTarget.distance) //check if the hit object is further that the hit on the gun plane
    //        {
    //            Debug.Log("Shooting to reticle");
    //            bulletDirection = reticleTarget.point - gunMuzzle.position;
    //        }
    //        else
    //        {
    //            if (Physics.Raycast(reticleTarget.point, ray.direction, out RaycastHit targetPastReticle, 999f)) //shoot the same ray but with the previously hit point as origin
    //            {
    //                furtherReticleDebugHit = targetPastReticle.point;

    //                Debug.Log("Shooting past reticle");
    //                bulletDirection = targetPastReticle.point - gunMuzzle.position;
    //            }
    //            else { Debug.Log("Aiming at infinite"); }
    //        }
    //    }
    //    else { Debug.Log("Aiming at infinite"); }

    //    return new PlayerAction(Wrappers.PlayerAction.ActionType.Shot, new List<string> { gunMuzzle.position.x.ToString(),
    //                                                                                               gunMuzzle.position.y.ToString(),
    //                                                                                               gunMuzzle.position.z.ToString(),
    //                                                                                               bulletDirection.x.ToString(),
    //                                                                                               bulletDirection.y.ToString(),
    //                                                                                               bulletDirection.z.ToString() });
    //}

    public void Shoot(Vector3 origin, Vector3 direction)
    {
        GameObject trail = (GameObject)Instantiate(Resources.Load("Prefabs/BulletTrail"));

        if (Physics.Raycast(origin, direction, out RaycastHit bulletHit, 999f))
        {
            lastDebugOrigin = gunMuzzle.position;
            debugHit = bulletHit.point; //This is where the bullet must land
            //Debug.Log($"Bullet hit {bulletHit.collider.gameObject.name}");

            trail.GetComponent<BulletTrail>().SetTrailPositions(gunMuzzle.position, bulletHit.point);

            //bulletHit.collider.GetComponent<BasicZombie>()?.TakeDamage(gunDamage);
        }
        else
        {
            Debug.Log("Bullet hit nothing.");
            trail.GetComponent<BulletTrail>().SetTrailPositions(gunMuzzle.position, gunMuzzle.forward * 999f);
        }
    }

    private void OnDrawGizmos()
    {
        //Green is muzzle to hitPoint
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