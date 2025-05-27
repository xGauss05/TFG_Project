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
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> steamName = new NetworkVariable<FixedString64Bytes>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Player Gun properties")]
    [SerializeField] Billboard billboard;
    [SerializeField] Transform gunPivot;
    public Transform camPivot;

    // Flags for logic handling
    bool isDead = false;

    // Helpers and Components
    public Inventory inventory;
    GameObject pistolObject;
    GameObject assaultRifleObject;
    GameObject shotgunObject;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        pistolObject = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Items/Guns/Pistol"), gunPivot);
        assaultRifleObject = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Items/Guns/AssaultRifle"), gunPivot);
        shotgunObject = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Items/Guns/Shotgun"), gunPivot);

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
            var playerUI = FindObjectOfType<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.SetPlayer(this);
            }

            EquipGunServerRpc(GunBase.Type.Pistol);
        }
    }

    void Update()
    {
        if (!IsOwner || isDead) return;

        if (IsOwner)
        {
            HandleInput();
        }
    }

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
            if (IsServer)
                inventory.ReloadGun();
            else
                ReloadServerRpc();
        }

        // Interact (atm, only door)
        if (Input.GetKeyDown(KeyCode.E) && FindDoor())
        {
            // Open door
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

        // Change gun
        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && !inventory.currentGun.isReloading) EquipGunClientRpc(GunBase.Type.Pistol);
            if (Input.GetKeyDown(KeyCode.Alpha2) && !inventory.currentGun.isReloading) EquipGunClientRpc(GunBase.Type.AssaultRifle);
            if (Input.GetKeyDown(KeyCode.Alpha3) && !inventory.currentGun.isReloading) EquipGunClientRpc(GunBase.Type.Shotgun);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && !inventory.currentGun.isReloading) EquipGunServerRpc(GunBase.Type.Pistol);
            if (Input.GetKeyDown(KeyCode.Alpha2) && !inventory.currentGun.isReloading) EquipGunServerRpc(GunBase.Type.AssaultRifle);
            if (Input.GetKeyDown(KeyCode.Alpha3) && !inventory.currentGun.isReloading) EquipGunServerRpc(GunBase.Type.Shotgun);
        }

        // Use Medkit
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (IsServer)
                UseMedkit();
            else
                UseMedkitServerRpc();
        }

    }

    void UseMedkit()
    {
        if (inventory.UseMedkit())
        {
            Debug.Log($"Before {currentHealth.Value}");
            currentHealth.Value += (int)(maxHealth - currentHealth.Value) * 80 / 100;
            Debug.Log($"After {currentHealth.Value}");
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

            pistolObject.SetActive(false);
            assaultRifleObject.SetActive(false);
            shotgunObject.SetActive(false);
            gunGO.SetActive(true);
        }
    }

    void OpenDoor()
    {
        // NYI
        //GameObject.Find("SafeRoomDoor").GetComponentInChildren<SafeRoomDoor>()?.Open(transform.position);
    }

    bool FindDoor()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
        {
            //return hit.collider.GetComponent<SafeRoomDoor>() != null;
        }

        return false;
    }

    void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes current)
    {
        billboard.SetName(current);
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void PlayHurtSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(playerHurtSfx);
    }

    [ClientRpc]
    void EquipGunClientRpc(GunBase.Type type)
    {
        switch (type)
        {
            case GunBase.Type.Pistol:
                EquipGun(pistolObject);
                break;
            case GunBase.Type.AssaultRifle:
                EquipGun(assaultRifleObject);
                break;
            case GunBase.Type.Shotgun:
                EquipGun(shotgunObject);
                break;
            case GunBase.Type.None:
            default:
                EquipGun(pistolObject);
                break;
        }
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
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
        EquipGunClientRpc(type);
    }

}
