using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class PickupItem : NetworkBehaviour
{
    [SerializeField] float startY = 0.0f;
    [SerializeField] float endY = 1.5f;
    [SerializeField] float upDownSpeed = 0.5f;
    [SerializeField] float rotSpeed = 10.0f;

    public bool useRandomSpawnPoint = true;

    public abstract void OnPickup(Player player);

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                OnPickup(player);
                DespawnServerRpc();
                //Debug.Log("Item pickup!");
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && useRandomSpawnPoint)
        {
            // Currently testing for ZombieSpawnpoints
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("ZombieSpawnpoint");

            if (spawnPoints.Length > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Length);
                Transform spawnTransform = spawnPoints[randomIndex].transform;

                // Set the PickupItem spawn point to the Spawnpoint position
                transform.position = spawnTransform.position;
                transform.rotation = spawnTransform.rotation;
            }
            else
            {
                Debug.LogWarning("No zombie spawn points found. Spawning at default position.");
                transform.position = new Vector3(0, 0, 0); // Default position
            }

        }
    }

    void Update()
    {
        float newY = Mathf.Lerp(startY, endY, Mathf.PingPong(Time.time * upDownSpeed, 1.0f));
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        Vector3 rot = new Vector3(0, rotSpeed * Time.deltaTime, 0);
        gameObject.transform.Rotate(rot);
    }

    [ServerRpc]
    void DespawnServerRpc()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}