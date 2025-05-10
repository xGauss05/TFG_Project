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

    //public NetworkVariable<string> ownerName = new NetworkVariable<string>();
    float moveSpeed = 5f;

    [SerializeField] float sensitivity = 100.0f;
    [SerializeField] float verticalRotation = 0f;
    const float maxAngle = 90f;

    [SerializeField] Transform gunPivot;
    public Transform camPivot;
    public bool lockCamera = false;

    //private Gun currentGun;

    void Awake()
    {
        //GameObject gunGO = (GameObject)Instantiate(Resources.Load("Prefabs/Gun"), gunPivot);
        //currentGun = gunGO.GetComponent<Gun>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            this.enabled = false;
        }
        else
        {
            // Currently hardcoded
            // Check player spawn points in map then choose 1 random
            this.transform.position = new Vector3(20, 2, -20);

            Camera.main.GetComponent<PlayerCamera>().SetParent(camPivot);
            Camera.main.transform.rotation = transform.rotation;
            GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        }
    }

    void Start()
    {
        //if (IsOwner)
        //{
        //    //Camera.main.GetComponent<PlayerCamera>().SetParent(camPivot);
        //    Camera.main.transform.rotation = transform.rotation;

        //    GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        //}
        //else
        //{
        //    //GetComponentInChildren<TMPro.TextMeshProUGUI>().text = ownerName.Value;
        //}
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKey(KeyCode.W)) SubmitMoveServerRpc(PlayerAction.MoveF);
        if (Input.GetKey(KeyCode.S)) SubmitMoveServerRpc(PlayerAction.MoveB);
        if (Input.GetKey(KeyCode.A)) SubmitMoveServerRpc(PlayerAction.MoveL);
        if (Input.GetKey(KeyCode.D)) SubmitMoveServerRpc(PlayerAction.MoveR);

        if (Input.GetKeyDown(KeyCode.E) && FindDoor())
        {
            SubmitActionServerRpc(PlayerAction.OpenDoor);
        }

        float mouseX = 0;
        float mouseY = 0;

        if (!lockCamera)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }

        if (mouseX != 0 || mouseY != 0)
        {
            SubmitRotateServerRpc(mouseX * sensitivity, mouseY * sensitivity);
        }

        if (Input.GetButtonDown("Fire1"))
        {
            var origin = Camera.main.transform.position;
            var dir = Camera.main.transform.forward;
            SubmitShotServerRpc(origin, dir);
        }
    }

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
        //GameObject.Find("SafeRoomDoor").GetComponentInChildren<SafeRoomDoor>()?.Open(transform.position);
    }

    bool FindDoor()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            //return hit.collider.GetComponent<SafeRoomDoor>() != null;
        }

        return false;
    }
}
