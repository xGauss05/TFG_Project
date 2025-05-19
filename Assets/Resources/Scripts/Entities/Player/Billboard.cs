using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Steamworks;

public class Billboard : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject nameCanvas;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            nameCanvas.gameObject.SetActive(true);

            if (IsClient && !IsServer)
            {
                RequestNameServerRpc(SteamClient.Name);
            }
        }
        else
        {
            nameCanvas.gameObject.SetActive(false);

            if (IsServer)
            {
                SetNameClientRpc(SteamClient.Name, OwnerClientId);
            }
        }
    }

    void Update()
    {
        if (Camera.main != null && nameCanvas != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void SetNameClientRpc(string steamName, ulong targetClientId)
    {
        if (OwnerClientId != targetClientId) return;

        nameText.text = steamName;
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc(RequireOwnership = false)]
    void RequestNameServerRpc(string steamName, ServerRpcParams rpcParams = default)
    {
        SetNameClientRpc(steamName, rpcParams.Receive.SenderClientId);
    }

}
