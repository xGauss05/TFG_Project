using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Steamworks;

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
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(maxHealth);
    NetworkVariable<FixedString64Bytes> steamName = new NetworkVariable<FixedString64Bytes>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Player Gun properties")]
    [SerializeField] Billboard billboard;
    [SerializeField] Transform gunPivot;
    public Transform camPivot;

    // Flags for logic handling
    bool isDead = false;

    // Helpers and Components
    GunBase currentGun;
    AudioSource audioSource;
    float x_networkIncrement;
    float y_networkIncrement;

    void Awake()
    {
        GameObject gunGO = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Pistol"), gunPivot);
        currentGun = gunGO.GetComponent<Pistol>();
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
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        audioSource = GetComponent<AudioSource>();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // Player movement
        PlayerAction action = PlayerAction.None;

        if (Input.GetKey(KeyCode.W)) action |= PlayerAction.MoveF;
        if (Input.GetKey(KeyCode.S)) action |= PlayerAction.MoveB;
        if (Input.GetKey(KeyCode.A)) action |= PlayerAction.MoveL;
        if (Input.GetKey(KeyCode.D)) action |= PlayerAction.MoveR;

        if (action != PlayerAction.None) SendMoveServerRpc(action);
    }

    void Update()
    {
        if (IsOwner)
        {
            HandleInput();
        }
        else
        {
            LocalViewRotate(x_networkIncrement, y_networkIncrement);

            x_networkIncrement = 0f;
            y_networkIncrement = 0f;
        }

        if (!IsOwner || isDead) return;

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
                currentGun.Reload();
            else
                ReloadServerRpc();
        }

        // Interact (atm, only door)
        if (Input.GetKeyDown(KeyCode.E) && FindDoor())
        {
            // Open door
        }

        // Use Medkit
        if (Input.GetKeyDown(KeyCode.H))
        {
            // Use medkit
        }

        // Mouse camera
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (mouseX != 0 || mouseY != 0)
        {
            float xIncrement = mouseX * sensitivity * Time.deltaTime;
            float yIncrement = mouseY * sensitivity * Time.deltaTime;
            LocalViewRotate(xIncrement, yIncrement);
            SendRotationServerRpc(xIncrement, yIncrement);
        }

        // Shoot weapon
        if (Input.GetButton("Fire1"))
        {
            var shot = currentGun.CalculateShot();
            if (IsServer)
            {
                currentGun.Shoot(shot.origin, shot.direction);
            }
            else
            {
                SubmitShotServerRpc(shot.origin, shot.direction);
            }
        }
    }

    void LocalViewRotate(float xIncrement, float yIncrement)
    {
        transform.Rotate(Vector3.up * xIncrement);

        verticalRotation += -yIncrement;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxAngle, maxAngle);

        camPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

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
            transform.Translate(direction * moveSpeed * deltaTime, Space.World);
        }
    }

    void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes current)
    {
        billboard.SetName(current);
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void SendRotationClientRpc(float xIncrement, float yIncrement)
    {
        if (IsOwner) return;

        x_networkIncrement = xIncrement;
        y_networkIncrement = yIncrement;
    }

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    void SendMoveServerRpc(PlayerAction move)
    {
        Move(move, Time.deltaTime);
    }

    [ServerRpc]
    void SendRotationServerRpc(float xIncrement, float yIncrement)
    {
        SendRotationClientRpc(xIncrement, yIncrement);
    }

    [ServerRpc]
    void SubmitShotServerRpc(Vector3 origin, Vector3 dir)
    {
        currentGun.Shoot(origin, dir);
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int amount)
    {
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= amount;
        audioSource.PlayOneShot(playerHurtSfx);

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Debug.Log("Player dead!");
        }
    }

    [ServerRpc]
    void ReloadServerRpc()
    {
        currentGun.Reload();
    }

}
