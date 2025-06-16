using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GraphGenerator : MonoBehaviour
{
    [SerializeField] TextAsset initialGraphJson;
    [SerializeField] int seed = 0;

    [SerializeField] List<ReplacementStep> stepsToFollow = new List<ReplacementStep>();

    public Graph activeGraph;
    public UnityEvent OnGraphComplete = new UnityEvent();

    private void Awake()
    {
        activeGraph = new Graph(JsonUtility.FromJson<Graph_Data>(initialGraphJson.text));
    }

    private void Start()
    {
        if (seed == 0) { seed = Random.Range(int.MinValue, int.MaxValue); }

        foreach (ReplacementStep step in stepsToFollow)
        {
            step.ApplyRules(activeGraph, seed);
        }

        OnGraphComplete.Invoke();
    }
}