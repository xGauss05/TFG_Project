using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ping : NetworkBehaviour
{
    [Header("Ping settings")]
    [SerializeField] float updateInterval = 0.1f;

    // Helpers
    float timer = 0.0f;

    public float rtt { get; private set; } = 0;

    int sentPings = 0;
    int receivedPings = 0;
    int lastPingId = 0;

    void Start()
    {
        var ui = FindObjectOfType<PingUI>();
        if (ui != null)
        {
            ui.SetPing(this);
        }
    }

    void Update()
    {
        if (!IsClient || !IsOwner) return;

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            lastPingId++;
            SendPingServerRpc(Time.realtimeSinceStartup, lastPingId, OwnerClientId);
            sentPings++;
            timer = 0f;
        }
    }

    public int GetPing()
    {
        return (int)(rtt / 2);
    }

    public int GetRtt()
    {
        return (int)rtt;
    }

    public float GetPacketLossPercentage()
    {
        if (sentPings == 0) return 0.0f;

        return 100.0f * (1.0f - ((float)receivedPings / sentPings));
    }

    public int GetPacketsLost()
    {
        return sentPings - receivedPings;
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void ReturnPingClientRpc(float clientTime, int pingId, ClientRpcParams rpcParams = default)
    {
        if (pingId <= lastPingId)
        {
            receivedPings++;
            rtt = Mathf.Abs(Time.realtimeSinceStartup - clientTime) * 1000;
        }
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    void SendPingServerRpc(float clientTime, int pingId, ulong clientId)
    {
        ReturnPingClientRpc(clientTime, pingId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
    }

}
