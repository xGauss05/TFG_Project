using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Vector3 offset;
    bool firstPerson = false;

    void Update()
    {
        if (transform.parent == null) { return; } //This means Player object has not been instantiated yet

        // First person perspective
        if (Input.GetKeyDown(KeyCode.V))
        {
            firstPerson = !firstPerson;

            if (firstPerson)
            {
                transform.localPosition = Vector3.zero;
            }
            else
            {
                transform.localPosition = offset;
            }
        }

        // Third person perspective
        if (!firstPerson)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                offset.x *= -1;
                transform.localPosition = offset;
            }
        }

        CheckWalls();
    }

    public void SetParent(Transform parent)
    {
        transform.parent = parent;

        transform.localPosition = offset;
    }

    void CheckWalls()
    {
        if (Physics.Raycast(transform.parent.position, transform.position - transform.parent.position, out RaycastHit raycasthit, offset.magnitude))
        {
            transform.position = raycasthit.point;
        }
        else
        {
            if (!firstPerson)
            {
                transform.localPosition = offset;
            }
        }
    }
}
