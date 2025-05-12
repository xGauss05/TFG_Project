using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] float trailLifeTime = 0.1f;
    [SerializeField] LineRenderer lineRenderer;
    float currentTime;

    void Awake()
    {
        currentTime = trailLifeTime;
    }

    void Update()
    {
        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            Destroy(gameObject);
        }

        Color currentColor = lineRenderer.material.GetColor("_Color");
        currentColor.a = currentTime / trailLifeTime;
        lineRenderer.material.SetColor("_Color", currentColor);
    }

    public void SetTrailPositions(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}