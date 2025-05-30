using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ApplicationVersion : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI versionObject;

    // Start is called before the first frame update
    void Start()
    {
        versionObject.text = Application.version;
    }
}
