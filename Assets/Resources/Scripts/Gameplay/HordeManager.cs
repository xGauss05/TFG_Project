using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HordeManager : NetworkBehaviour
{
    public static HordeManager Singleton { get; private set; }

    [Header("Horde settings")]
    [SerializeField] float hordeDuration = 60.0f;
    float hordeTimer = 0.0f;

    NetworkVariable<bool> isHordeActive = new NetworkVariable<bool>(false);
    public bool IsHordeActive => isHordeActive.Value;

    void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Singleton = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
    }

    void Update()
    {
        if (!IsServer) return;

        if (isHordeActive.Value)
        {
            hordeTimer += Time.deltaTime;

            if (hordeTimer >= hordeDuration)
            {
                StopHorde();
            }
        }
    }

    public void StartHorde()
    {
        if (!IsServer) return;

        isHordeActive.Value = true;
        hordeTimer = 0.0f;
        Debug.Log("Horde started!");
    }

    public void StopHorde()
    {
        if (!IsServer) return;

        isHordeActive.Value = false;
        hordeTimer = 0.0f;
        Debug.Log("Horde ended.");
    }
}
