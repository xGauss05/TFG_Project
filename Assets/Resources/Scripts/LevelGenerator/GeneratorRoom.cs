using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    North,
    East,
    South,
    West
}

[System.Serializable]
public class Entrance
{
    public Direction direction;
    public Vector2Int localPosition;
    public GameObject blockage;
    [HideInInspector] public bool unlocked;
}
public class GeneratorRoom : MonoBehaviour
{
    public Vector2Int size;
    public List<Entrance> entrances;

    private void Awake()
    {
        foreach (var entrance in entrances)
        {
            entrance.unlocked = false;
        }
    }
}
