using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieDamageHitbox : MonoBehaviour
{
    public BasicZombie attacker;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (attacker != null)
            {
                other.GetComponent<Player>().TakeDamageServerRpc(attacker.attackDamage);
            }
        }
    }

    void OnDrawGizmos()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();

        Gizmos.color = Color.red;

        Vector3 worldCenter = transform.TransformPoint(sphere.center);
        float worldRadius = sphere.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        );

        Gizmos.DrawWireSphere(worldCenter, worldRadius);
    }
}
