using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PingUI : MonoBehaviour
{
    [Header("Ping UI properties")]
    [SerializeField] Ping ping;
    [SerializeField] TextMeshProUGUI pingText;

    void Update()
    {
        if (ping == null || pingText == null) return;

        int rtt = (int)ping.rtt;

        if (rtt <= 80) pingText.color = Color.green;
        else if (rtt <= 150) pingText.color = Color.yellow;
        else pingText.color = Color.red;

        pingText.text = $"Ping: {rtt} ms";
    }
}
