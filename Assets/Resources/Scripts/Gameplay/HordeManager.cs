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

    [Header("Horde Audios")]
    [SerializeField] AudioClip hordeScreamSfx;

    [Header("UI Components")]
    [SerializeField] GameObject adrenalineIndicator;

    NetworkVariable<bool> isHordeActive = new NetworkVariable<bool>(false);

    List<ZombieHordeSpawner> hordeSpawners = new List<ZombieHordeSpawner>();

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

        hordeSpawners.Clear();
        hordeSpawners.AddRange(FindObjectsOfType<ZombieHordeSpawner>());

        StartCoroutine(TriggerHorde());
    }

    IEnumerator TriggerHorde()
    {
        yield return new WaitForSeconds(1.0f);

        isHordeActive.Value = true;
        hordeTimer = 0.0f;
        PlayHordeScreamClientRpc();
        ToggleAdrenalineUIClientRpc(true);
        Debug.Log("Horde started!");
    }

    public void StopHorde()
    {
        if (!IsServer) return;

        isHordeActive.Value = false;
        hordeTimer = 0.0f;
        ToggleAdrenalineUIClientRpc(false);

        foreach (ZombieHordeSpawner spawner in hordeSpawners)
        {
            if (spawner != null)
            {
                spawner.ResetCount();
            }
        }

        Debug.Log("Horde ended.");
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void PlayHordeScreamClientRpc()
    {
        SFXManager.Singleton.PlaySound(hordeScreamSfx);
    }

    [ClientRpc]
    void ToggleAdrenalineUIClientRpc(bool active)
    {
        adrenalineIndicator.SetActive(active);
    }
}
