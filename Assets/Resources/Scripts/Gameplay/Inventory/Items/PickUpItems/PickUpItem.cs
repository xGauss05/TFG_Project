using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class PickupItem : NetworkBehaviour
{
    [Header("PickUp Item Parameters")]
    [SerializeField] float startY = 0.0f;
    [SerializeField] float endY = 1.5f;
    [SerializeField] float upDownSpeed = 0.5f;
    [SerializeField] float rotSpeed = 10.0f;

    [Header("PickUp Audios")]
    [SerializeField] AudioClip pickupSfx;

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

                PlayPickupSfxClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { player.OwnerClientId }
                    }
                });

                DespawnServerRpc();
                //Debug.Log("Item pickup!");
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

    [ClientRpc]
    void PlayPickupSfxClientRpc(ClientRpcParams rpcParams = default)
    {
        SFXManager.Singleton.PlaySound(pickupSfx);
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