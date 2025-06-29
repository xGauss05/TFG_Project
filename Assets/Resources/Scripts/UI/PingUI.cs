using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PingUI : MonoBehaviour
{
    [Header("Ping UI properties")]
    [SerializeField] Ping ping;
    [SerializeField] TextMeshProUGUI pingText;

    public void SetPing(Ping ping)
    {
        this.ping = ping;
    }

    void Update()
    {
        if (ping == null || pingText == null) return;

        if (ping.GetPing() <= 100) pingText.color = Color.green;
        else if (ping.GetPing() <= 150) pingText.color = Color.yellow;
        else pingText.color = Color.red;

        pingText.text = $"Ping: {ping.GetPing()} ms\n" +
            $"RTT: {ping.GetRtt()} ms\n" +
            $"Packets Lost: {ping.GetPacketsLost()} ({ping.GetPacketLossPercentage()} %)";
    }
}
