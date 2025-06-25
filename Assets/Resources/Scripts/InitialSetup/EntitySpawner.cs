using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntitySpawner : NetworkBehaviour
{
    [SerializeField] GameObject AmmoBox;
    [SerializeField] GameObject AssaultRifle;
    [SerializeField] GameObject Box;
    [SerializeField] GameObject Car;
    [SerializeField] GameObject CarAlarmed;
    [SerializeField] GameObject Door;
    [SerializeField] GameObject Medkit;
    [SerializeField] GameObject Shotgun;

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
            SpawnEntityWithTag("AmmoBoxSpawn", AmmoBox);
            SpawnEntityWithTag("AssaultRifleSpawn", AssaultRifle);
            SpawnEntityWithTag("BoxSpawn", Box);
            SpawnEntityWithTag("CarSpawn", Car);
            SpawnEntityWithTag("CarAlarmSpawn", CarAlarmed);
            SpawnEntityWithTag("DoorSpawn", Door);
            SpawnEntityWithTag("MedkitSpawn", Medkit);
            SpawnEntityWithTag("ShotgunSpawn", Shotgun);
        }
    }

    void SpawnEntityWithTag(string spawnTag, GameObject entityPrefab)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(spawnTag);

        foreach (var spawnPoint in spawnPoints)
        {
            GameObject entity = Instantiate(entityPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            entity.GetComponent<NetworkObject>().Spawn();
        }
    }
}
