using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
    [SerializeField] float radius;
    [SerializeField] float speed;

    [Range(0f, 360f)] public float initialAngle = 0f;
    private float angle = 0f;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        angle = initialAngle * Mathf.Deg2Rad;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        rectTransform.anchoredPosition = new Vector2(x, y);
    }
}
