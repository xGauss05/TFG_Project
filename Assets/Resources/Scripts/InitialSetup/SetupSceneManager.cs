using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SetupSceneManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadMainmenuScene());
    }

    IEnumerator LoadMainmenuScene()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        SceneManager.LoadScene("1_MainMenu");
    }
}
