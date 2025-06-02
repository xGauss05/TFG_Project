using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [System.Flags]
    public enum PlayerAction
    {
        None = 0,
        MoveF = 1 << 0,
        MoveB = 1 << 1,
        MoveL = 1 << 2,
        MoveR = 1 << 3
    }

    [Header("Player Parameters")]
    [SerializeField] float moveSpeed = 5.0f;
    [SerializeField] float sensitivity = 100.0f;
    [SerializeField] float verticalRotation = 0.0f;
    const float maxAngle = 90.0f;
    const int maxHealth = 100;

    [Header("Player Audios")]
    [SerializeField] AudioClip playerHurtSfx;

    [Header("Player Network variables")]
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        maxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> steamName = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    [Header("Player Gun properties")]
    [SerializeField] Billboard billboard;
    [SerializeField] Transform gunPivot;
    public Transform camPivot;

    [Header("Player Particles")]
    [SerializeField] GameObject ps_bloodSplatter;

    // Flags for logic handling
    bool isDead = false;

    // Helpers and Components
    public Inventory inventory;
    [SerializeField] GameObject pistolObject;
    [SerializeField] GameObject assaultRifleObject;
    [SerializeField] GameObject shotgunObject;
    Door doorNearby;

    // Unity Event functions ------------------------------------------------------------------------------------------
    #region Unity Event functions
    void Awake()
    {
        //inventory = GetComponent<Inventory>();

        inventory.availableGuns.Add(pistolObject.GetComponent<Pistol>(), true);
        inventory.availableGuns.Add(assaultRifleObject.GetComponent<AssaultRifle>(), false);
        inventory.availableGuns.Add(shotgunObject.GetComponent<Shotgun>(), false);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Camera.main.GetComponent<PlayerCamera>().SetParent(camPivot);
            Camera.main.transform.rotation = transform.rotation;
            GetComponentInChildren<Canvas>().gameObject.SetActive(false);
            steamName.Value = SteamClient.Name;
        }

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawnpoint");

        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnTransform = spawnPoints[randomIndex].transform;

            // Set the Player spawn point to the Spawnpoint position
            transform.position = spawnTransform.position;
            transform.rotation = spawnTransform.rotation;
        }
        else
        {
            Debug.LogWarning("No player spawn points found. Spawning at default position.");
            Vector3 defaultPos = new Vector3(20, 2, -20);
            this.transform.position = defaultPos;
        }

        billboard.SetName(steamName.Value);
        steamName.OnValueChanged += OnNameChanged;

        if (IsLocalPlayer)
        {
            EquipGunServerRpc(GunBase.Type.Pistol);

            var playerUI = FindObjectOfType<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.SetPlayer(this);
            }

        }
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned || isDead) return;

        if (IsOwner)
        {
            HandleInput();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Door door = other.GetComponent<Door>();
        if (door != null)
        {
            doorNearby = door;
        }
    }

    void OnTriggerExit(Collider other)
    {
        Door door = other.GetComponent<Door>();
        if (door != null && doorNearby == door)
        {
            doorNearby = null;
        }
    }
    #endregion

    void HandleInput()
    {
        // Change cursor mode
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        // Interact
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryToggleDoor(doorNearby);
        }

        // Player movement
        PlayerAction action = PlayerAction.None;

        if (Input.GetKey(KeyCode.W)) action |= PlayerAction.MoveF;
        if (Input.GetKey(KeyCode.S)) action |= PlayerAction.MoveB;
        if (Input.GetKey(KeyCode.A)) action |= PlayerAction.MoveL;
        if (Input.GetKey(KeyCode.D)) action |= PlayerAction.MoveR;

        if (action != PlayerAction.None) Move(action, Time.deltaTime);

        // Mouse camera
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (mouseX != 0 || mouseY != 0)
        {
            float xIncrement = mouseX * sensitivity * Time.deltaTime;
            float yIncrement = mouseY * sensitivity * Time.deltaTime;
            Rotate(xIncrement, yIncrement);
        }

        // Shoot weapon
        if (Input.GetButton("Fire1"))
        {
            TryShoot();
        }

        // Change gun
        if (Input.GetKeyDown(KeyCode.Alpha1) && !inventory.currentGun.isReloading) TryChangeGun(GunBase.Type.Pistol);
        if (Input.GetKeyDown(KeyCode.Alpha2) && !inventory.currentGun.isReloading) TryChangeGun(GunBase.Type.AssaultRifle);
        if (Input.GetKeyDown(KeyCode.Alpha3) && !inventory.currentGun.isReloading) TryChangeGun(GunBase.Type.Shotgun);

        // Use Medkit
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TryMedkit();
        }

    }

    void Move(PlayerAction actions, float deltaTime)
    {
        Vector3 direction = Vector3.zero;

        if (actions.HasFlag(PlayerAction.MoveF)) direction += transform.forward;
        if (actions.HasFlag(PlayerAction.MoveB)) direction -= transform.forward;
        if (actions.HasFlag(PlayerAction.MoveL)) direction -= transform.right;
        if (actions.HasFlag(PlayerAction.MoveR)) direction += transform.right;

        if (direction.sqrMagnitude > 0.0f)
        {
            direction.Normalize();
            transform.position += direction * moveSpeed * deltaTime;
        }
    }

    void Rotate(float xIncrement, float yIncrement)
    {
        transform.Rotate(Vector3.up * xIncrement);

        verticalRotation += -yIncrement;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxAngle, maxAngle);

        camPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

    }

    void EquipGun(GameObject gunGO)
    {
        GunBase gunBase = gunGO.GetComponent<GunBase>();
        if (inventory.IsGunAvailable(gunBase))
        {
            inventory.EquipGun(gunBase);

            var pistolObjectRenderers = pistolObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in pistolObjectRenderers)
            {
                renderer.enabled = false;
            }

            var arObjectRenderers = assaultRifleObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in arObjectRenderers)
            {
                renderer.enabled = false;
            }

            var shotgunObjectRenderers = shotgunObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in shotgunObjectRenderers)
            {
                renderer.enabled = false;
            }

            var gunGORenderers = gunGO.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in gunGORenderers)
            {
                renderer.enabled = true;
            }
        }
    }

    void UseMedkit()
    {
        if (inventory.UseMedkit())
        {
            Debug.Log($"Before {currentHealth.Value}");
            int healthAmount = (int)(maxHealth - currentHealth.Value) * 80 / 100;
            currentHealth.Value += healthAmount;

            if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.SubstractScore(healthAmount);

            Debug.Log($"After {currentHealth.Value}");
        }
    }

    void TryShoot()
    {
        var shot = inventory.currentGun.CalculateShot();
        if (IsServer)
        {
            inventory.ShootGun(shot.origin, shot.direction);
            //inventory.currentGun.Shoot(shot.origin, shot.direction);
        }
        else
        {
            SubmitShotServerRpc(shot.origin, shot.direction);
        }
    }

    void TryChangeGun(GunBase.Type type)
    {
        if (IsServer)
        {
            EquipGunClientRpc(type);
        }
        else
        {
            EquipGunServerRpc(type);
        }
    }

    void TryReload()
    {
        if (IsServer)
        {
            inventory.ReloadGun();
        }
        else
        {
            ReloadServerRpc();
        }
    }

    void TryMedkit()
    {
        if (IsServer)
        {
            UseMedkit();
        }
        else
        {
            UseMedkitServerRpc();
        }
    }

    void TryToggleDoor(Door door)
    {
        if (door != null)
        {
            if (IsServer)
            {
                ToggleDoorClientRpc(door.NetworkObject);
            }
            else
            {
                ToggleDoorServerRpc(door.NetworkObject);
            }
        }
    }

    void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes current)
    {
        billboard.SetName(current);
    }

    public GunBase GetGunBaseComponent(GunBase.Type type)
    {
        switch (type)
        {
            case GunBase.Type.AssaultRifle:
                return assaultRifleObject.GetComponent<AssaultRifle>();
            case GunBase.Type.Shotgun:
                return shotgunObject.GetComponent<Shotgun>();
            case GunBase.Type.Pistol:
            case GunBase.Type.None:
            default:
                return pistolObject.GetComponent<Pistol>();
        }
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    #region Client RPC functions
    [ClientRpc]
    void PlayHurtSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(playerHurtSfx);
    }

    [ClientRpc]
    void EquipGunClientRpc(GunBase.Type type)
    {
        Debug.Log("Calling EquipGunClientRpc.");
        switch (type)
        {
            case GunBase.Type.AssaultRifle:
                EquipGun(assaultRifleObject);
                break;
            case GunBase.Type.Shotgun:
                EquipGun(shotgunObject);
                break;
            case GunBase.Type.Pistol:
            case GunBase.Type.None:
            default:
                EquipGun(pistolObject);
                break;
        }
    }

    [ClientRpc]
    void ToggleDoorClientRpc(NetworkObjectReference doorRef)
    {
        if (doorRef.TryGet(out NetworkObject doorObj))
        {
            Door door = doorObj.GetComponent<Door>();
            if (door != null)
            {
                door.Toggle();
            }
        }
    }

    [ClientRpc]
    void SpawnBloodEffectClientRpc()
    {
        if (ps_bloodSplatter == null) return;

        GameObject blood = Instantiate(ps_bloodSplatter, transform.position + Vector3.up * 1.0f, Quaternion.identity);
        Destroy(blood, 0.5f);
    }
    #endregion

    // Server RPC functions -------------------------------------------------------------------------------------------
    #region Server RPC functions
    [ServerRpc]
    void SubmitShotServerRpc(Vector3 origin, Vector3 dir)
    {
        inventory.ShootGun(origin, dir);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= amount;
        PlayHurtSFXClientRpc();
        SpawnBloodEffectClientRpc();

        if (IsServer && ScoreManager.Singleton != null) ScoreManager.Singleton.SubstractScore(amount);

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            isDead = true;
            Debug.Log($"Player {steamName.Value} dead!");
        }
    }

    [ServerRpc]
    void ReloadServerRpc()
    {
        inventory.ReloadGun();
    }

    [ServerRpc]
    void UseMedkitServerRpc()
    {
        UseMedkit();
    }

    [ServerRpc]
    void EquipGunServerRpc(GunBase.Type type)
    {
        Debug.Log("Calling EquipGunServerRpc.");
        EquipGunClientRpc(type);
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleDoorServerRpc(NetworkObjectReference doorRef)
    {
        ToggleDoorClientRpc(doorRef);
    }
    #endregion

}
