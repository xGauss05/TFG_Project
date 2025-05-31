using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Door : NetworkBehaviour
{
    [SerializeField] BoxCollider triggerA;
    [SerializeField] BoxCollider triggerB;
    [SerializeField] Transform doorTransform;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 2f;

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

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            doorTransform.rotation = Quaternion.Slerp(startRotation, closedRotation, time);
            yield return null;
        }

        doorTransform.rotation = closedRotation;
    }
}
