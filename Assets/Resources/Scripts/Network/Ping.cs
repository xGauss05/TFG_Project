using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ping : NetworkBehaviour
{
    [Header("Ping settings")]
    [SerializeField] float updateInterval = 0.5f;

    // Helpers
    float timer = 0.0f;

    public float rtt { get; private set; } = 0;

    void Update()
    {
        if (!IsClient || !IsOwner) return;

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            SendPingServerRpc(Time.realtimeSinceStartup);
            timer = 0f;
        }
    }

    [ServerRpc]
    void SendPingServerRpc(float clientTime)
    {
        ReturnPingClientRpc(clientTime);
    }

    [ClientRpc]
    void ReturnPingClientRpc(float clientTime)
    {
        rtt = (ulong)Mathf.Abs(Time.realtimeSinceStartup - clientTime);
    }
}
