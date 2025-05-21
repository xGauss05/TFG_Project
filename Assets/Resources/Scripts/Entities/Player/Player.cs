using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
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

    [Header("Player Gun properties")]
    [SerializeField] Transform gunPivot;
    public Transform camPivot;

    // Flags for logic handling
    bool isDead = false;

    // Helpers and Components
    GunBase currentGun;
    AudioSource audioSource;
    Vector3 networkPosition;
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
            this.transform.position = new Vector3(20, 2, -20);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {

        if (IsOwner)
        {
            HandleInput();
        }
        else
        {
            transform.position = networkPosition;
            LocalViewRotate(x_networkIncrement, y_networkIncrement);
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
            currentGun.Reload();
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

        // Player movement
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
            SendPositionServerRpc(transform.position);
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
            SubmitShotServerRpc(shot.origin, shot.direction);
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

    // Server RPC functions -------------------------------------------------------------------------------------------
    [ServerRpc]
    void SendPositionServerRpc(Vector3 position)
    {
        SendPositionClientRpc(position);
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

    // Client RPC functions -------------------------------------------------------------------------------------------
    [ClientRpc]
    void SendPositionClientRpc(Vector3 position)
    {
        if (IsOwner) return;

        networkPosition = position;
    }

    [ClientRpc]
    void SendRotationClientRpc(float xIncrement, float yIncrement)
    {
        if (IsOwner) return;

        x_networkIncrement = xIncrement;
        y_networkIncrement = yIncrement;
    }

}
