using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Door : NetworkBehaviour
{
    [SerializeField] Transform doorTransform;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 2f;

    [Header("Door Audios")]
    [SerializeField] AudioClip openDoorSfx;
    [SerializeField] AudioClip closeDoorSfx;

    public bool isOpen { get; private set; } = false;

    Quaternion closedRotation;
    Quaternion openRotation;

    // Start is called before the first frame update
    void Start()
    {
        closedRotation = doorTransform.rotation;

        openRotation = closedRotation * Quaternion.Euler(0, 0, openAngle);
    }

    public void Toggle()
    {
        if (isOpen)
        {
            StartCoroutine(CloseDoor());
        }
        else
        {
            StartCoroutine(OpenDoor());
        }
    }

    IEnumerator OpenDoor()
    {
        isOpen = true;
        float time = 0f;
        Quaternion startRotation = doorTransform.rotation;
        OpenDoorSFXClientRpc();

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            doorTransform.rotation = Quaternion.Slerp(startRotation, openRotation, time);
            yield return null;
        }

        doorTransform.rotation = openRotation;
    }

    IEnumerator CloseDoor()
    {
        isOpen = false;
        float time = 0f;
        Quaternion startRotation = doorTransform.rotation;
        CloseDoorSFXClientRpc();

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            doorTransform.rotation = Quaternion.Slerp(startRotation, closedRotation, time);
            yield return null;
        }

        doorTransform.rotation = closedRotation;
    }

    // Client RPC functions -------------------------------------------------------------------------------------------
    #region Client RPC functions
    [ClientRpc]
    void OpenDoorSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(openDoorSfx);
    }

    [ClientRpc]
    void CloseDoorSFXClientRpc()
    {
        SFXManager.Singleton.PlaySound(closeDoorSfx);
    }
    #endregion
}
