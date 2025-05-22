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

    public void InitForNetwork(Player player)
    {
        if (player.IsOwner)
        {
            nameText.text = SteamClient.Name;
            nameCanvas.SetActive(true);
        }
        else
        {
            nameText.text = $"Player {player.OwnerClientId}";
            nameCanvas.SetActive(true);
        }
    }

    void Update()
    {
        if (Camera.main != null && nameCanvas != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }

}
