using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Steamworks;
using Unity.Collections;
using System;

public class Billboard : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject nameCanvas;

    void Update()
    {
        if (Camera.main != null && nameCanvas != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }

    public void SetName(FixedString64Bytes current)
    {
        if (!IsOwner)
        {
            nameText.text = current.ConvertToString();
            nameCanvas.SetActive(true);
        }
    }
}
