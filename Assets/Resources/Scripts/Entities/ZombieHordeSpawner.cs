using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ZombieHordeSpawner : NetworkBehaviour
{
    [SerializeField] GameObject Zombie_Basic;
    const int maxSpawns = 3;
    const float timeBetweenSpawns = 2.0f;

    float spawnTimer = 0.0f;
    int spawnCount = 0;
    bool activateSpawn = false;

    void Start()
    {
        if (!IsHost) this.enabled = false;
    }

    void Update()
    {
        if (!IsHost || !activateSpawn || spawnCount >= maxSpawns) return;

        if (HordeManager.Singleton == null || !HordeManager.Singleton.IsHordeActive) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= timeBetweenSpawns && spawnCount < maxSpawns)
        {
            spawnTimer = 0.0f;
            SpawnZombie();
            spawnCount++;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activateSpawn = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activateSpawn = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activateSpawn = false;
        }
    }

    void SpawnZombie()
    {
        GameObject go = Instantiate(Zombie_Basic, transform.position, transform.rotation);
        BasicZombie bZombie = go.GetComponent<BasicZombie>();
        bZombie.aggressive = true;

        go.GetComponent<NetworkObject>().Spawn();

    }

    public void ResetCount()
    {
        spawnCount = 0;
    }
}
