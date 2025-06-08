using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZombieIdleSpawner : NetworkBehaviour
{
    [SerializeField] GameObject Zombie_Basic;
    [SerializeField] GameObject Zombie_Fast;
    [SerializeField] GameObject Zombie_Boss;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsHost && sceneName == "2_Gameplay")
        {
            SpawnZombiesWithTag("BasicZombieSpawnpoint", Zombie_Basic);
            SpawnZombiesWithTag("FastZombieSpawnpoint", Zombie_Fast);
            SpawnZombiesWithTag("BossZombieSpawnpoint", Zombie_Boss);
        }
    }

    void SpawnZombiesWithTag(string spawnTag, GameObject zombiePrefab)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(spawnTag);

        foreach (var spawnPoint in spawnPoints)
        {
            GameObject zombie = Instantiate(zombiePrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            zombie.GetComponent<NetworkObject>().Spawn();
        }
    }
}
