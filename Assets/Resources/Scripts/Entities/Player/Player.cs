using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    public enum PlayerAction
    {
        None,
        MoveF,
        MoveB,
        MoveL,
        MoveR,
        Rotate,
        Shot,
        OpenDoor,
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

    [Header("Player Gun properties")]
    [SerializeField] Transform gunPivot;
    public Transform camPivot;

    // Flags for logic handling
    bool isDead = false;

    // Helpers and Components
    GunBase currentGun;
    AudioSource audioSource;

    void Awake()
    {
        GameObject gunGO = (GameObject)Instantiate(Resources.Load("Prefabs/Gameplay/Pistol"), gunPivot);
        currentGun = gunGO.GetComponent<Pistol>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            this.enabled = false;
            //GetComponentInChildren<Canvas>().gameObject.SetActive(false);
            //GetComponentInChildren<TMPro.TextMeshProUGUI>().text = ownerName.Value;
        }
        else
        {
            Camera.main.GetComponent<PlayerCamera>().SetParent(camPivot);
            Camera.main.transform.rotation = transform.rotation;
            GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        }

        // Currently hardcoded
        // Should check player spawn points in map then choose 1 random
        this.transform.position = new Vector3(20, 2, -20);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsOwner || isDead) return;

        HandleInput();
    }

    void HandleInput()
    {
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

        if (Input.GetKey(KeyCode.W)) SubmitMoveServerRpc(PlayerAction.MoveF);
        if (Input.GetKey(KeyCode.S)) SubmitMoveServerRpc(PlayerAction.MoveB);
        if (Input.GetKey(KeyCode.A)) SubmitMoveServerRpc(PlayerAction.MoveL);
        if (Input.GetKey(KeyCode.D)) SubmitMoveServerRpc(PlayerAction.MoveR);

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentGun.Reload();
        }

        if (Input.GetKeyDown(KeyCode.E) && FindDoor())
        {
            SubmitActionServerRpc(PlayerAction.OpenDoor);
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (mouseX != 0 || mouseY != 0)
        {
            SubmitRotateServerRpc(mouseX * sensitivity, mouseY * sensitivity);
        }

        if (Input.GetButtonDown("Fire1"))
        {
            var shot = currentGun.CalculateShot();
            SubmitShotServerRpc(shot.origin, shot.direction);
        }
    }

    void Move(PlayerAction action, float deltaTime)
    {
        Vector3 direction = Vector3.zero;

        switch (action)
        {
            case PlayerAction.MoveF:
                direction = transform.forward;
                break;
            case PlayerAction.MoveB:
                direction = -transform.forward;
                break;
            case PlayerAction.MoveL:
                direction = -transform.right;
                break;
            case PlayerAction.MoveR:
                direction = transform.right;
                break;
        }

        if (direction.magnitude > 0)
        {
            transform.Translate(direction * moveSpeed * deltaTime, Space.World);
        }
    }

    void Rotate(float xIncrement, float yIncrement)
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
    void SubmitMoveServerRpc(PlayerAction actionType)
    {
        Move(actionType, Time.deltaTime);
    }

    [ServerRpc]
    void SubmitRotateServerRpc(float mouseX, float mouseY)
    {
        Rotate(mouseX * Time.deltaTime, mouseY * Time.deltaTime);
    }

    [ServerRpc]
    void SubmitShotServerRpc(Vector3 origin, Vector3 dir)
    {
        //currentGun.Shoot(origin, dir);
    }

    [ServerRpc]
    void SubmitActionServerRpc(PlayerAction actionType)
    {
        if (actionType == PlayerAction.OpenDoor)
        {
            OpenDoor();
        }
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
}
