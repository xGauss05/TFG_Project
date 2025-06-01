using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollReset : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float minY = -50f;
    public float maxY = 430f;

    void Update()
    {
        Vector2 pos = scrollRect.content.anchoredPosition;

        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        scrollRect.content.anchoredPosition = pos;
    }
}
