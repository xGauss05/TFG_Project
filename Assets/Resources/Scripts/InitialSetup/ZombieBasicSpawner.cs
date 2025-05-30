using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZombieBasicSpawner : NetworkBehaviour
{
    [SerializeField] GameObject Zombie_Basic;
    [SerializeField] GameObject Medkit;
    [SerializeField] GameObject AmmoBox;
    [SerializeField] GameObject Shotgun;
    [SerializeField] GameObject AssaultRifle;

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
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("ZombieSpawnpoint");

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                GameObject zombie = Instantiate(Zombie_Basic);
                GameObject medkit = Instantiate(Medkit);
                GameObject ammo = Instantiate(AmmoBox);
                GameObject ar = Instantiate(AssaultRifle);
                GameObject shotgun = Instantiate(Shotgun);

                zombie.GetComponent<NetworkObject>().Spawn();
                medkit.GetComponent<NetworkObject>().Spawn();
                ammo.GetComponent<NetworkObject>().Spawn();
                ar.GetComponent<NetworkObject>().Spawn();
                shotgun.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
  