using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] float trailLifeTime = 0.1f;
    [SerializeField] LineRenderer lineRenderer;
    float currentTime;

    private void Awake()
    {
        currentTime = trailLifeTime;
    }

    private void Update()
    {
        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            Destroy(gameObject);
        }

        lineRenderer.material.SetColor("_Color", new Color(1f, 1f, 1f, currentTime / trailLifeTime));
    }

    public void SetTrailPositions(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}