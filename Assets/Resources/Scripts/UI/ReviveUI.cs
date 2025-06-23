using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReviveUI : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;

    Player player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetPlayer(Player p)
    {
        player = p;
        if (player == null) return;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
