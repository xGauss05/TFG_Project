using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ping : MonoBehaviour
{
    [Header("Ping settings")]
    [SerializeField] float updateInterval = 0.5f;

    // Helpers
    float timer = 0.0f;

    public ulong rtt { get; private set; } = 0;

    void Update()
    {

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            UpdatePing();
            timer = 0f;
        }
    }

    void UpdatePing()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId);
    }
}
