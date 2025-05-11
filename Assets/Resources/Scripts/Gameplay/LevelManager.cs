using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] ExtractionZone exit;
    
    // Update is called once per frame
    void Update()
    {
        if (exit.playersInside >= exit.requiredPlayers)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Single);
            NetworkManager.Singleton.Shutdown();
        }
    }
}
