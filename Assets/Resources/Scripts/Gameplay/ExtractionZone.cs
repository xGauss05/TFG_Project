using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExtractionZone : MonoBehaviour
{
    public int playersInside;
    public int requiredPlayers;

    void Start()
    {
        requiredPlayers = LobbyReference.Singleton.currentLobby.Value.MemberCount;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        playersInside++;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        playersInside--;
    }
}
