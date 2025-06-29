using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeneratorCollisionSolver
{
    static void FindLimits(Vector2Int conflictPoint, out Vector2Int origin, out Vector2Int size, Cell?[,] grid)
    {
        Vector2Int currentEvalPos;
        Vector2Int retOrigin = Vector2Int.zero;
        Vector2Int retSize = Vector2Int.zero;

        //Find Pivot x
        currentEvalPos = conflictPoint;
        while (grid[currentEvalPos.x, currentEvalPos.y] != null) { currentEvalPos.x--; }
        retOrigin.x = currentEvalPos.x + 1;

        //Find Pivot y
        currentEvalPos = conflictPoint;
        while (grid[currentEvalPos.x, currentEvalPos.y] != null) { currentEvalPos.y--; }
        retOrigin.y = currentEvalPos.y + 1;

        //Return pivot
        origin = retOrigin;

        //Find Size x
        currentEvalPos = retOrigin;
        while (grid[currentEvalPos.x, currentEvalPos.y] != null) { currentEvalPos.x++; }
        retSize.x = currentEvalPos.x - retOrigin.x;

        //Find Size y
        currentEvalPos = retOrigin;
        while (grid[currentEvalPos.x, currentEvalPos.y] != null) { currentEvalPos.y++; }
        retSize.y = currentEvalPos.y - retOrigin.y;

        //Return size
        size = retSize;
    }

    static RectInt FindOverlappingArea(RectInt placingRect, Cell?[,] grid)
    {
        List<int> overlappingXs = new List<int>();
        List<int> overlappingYs = new List<int>();

        for (int i = -1; i < placingRect.size.x + 1; i++)
        {
            for (int j = -1; j < placingRect.size.y + 1; j++)
            {
                if (grid[placingRect.position.x + i, placingRect.position.y + j] != null)
                {
                    overlappingXs.Add(placingRect.position.x + i);
                    overlappingYs.Add(placingRect.position.y + j);
                }
            }
        }

        int[] arrayXs = overlappingXs.ToArray();
        int[] arrayYs = overlappingYs.ToArray();

        int minX = Mathf.Min(arrayXs);
        int maxX = Mathf.Max(arrayXs);
        int minY = Mathf.Min(arrayYs);
        int maxY = Mathf.Max(arrayYs);

        int xDiff = maxX - minX;
        int yDiff = maxY - minY;

        return new RectInt(minX, minY, xDiff, yDiff);
    }

    static void SolveCollision(ref Vector2Int origin, Vector2Int size, Vector2Int conflictPoint, Cell?[,] grid)
    {
        Vector2Int targetOrigin;
        Vector2Int targetSize;

        FindLimits(conflictPoint, out targetOrigin, out targetSize, grid);

        RectInt placingRect = new RectInt(origin, size);
        RectInt targetRect = new RectInt(targetOrigin, targetSize);

        targetRect.position -= Vector2Int.one;
        targetRect.size += Vector2Int.one * 2;

        RectInt overlappingRect = FindOverlappingArea(placingRect, grid);

        bool solveHorizontal = (overlappingRect.width <= overlappingRect.height) ? true : false;

        Vector2 dir = overlappingRect.center - targetRect.center;

        if (solveHorizontal)
        {
            if (dir.x > 0) origin.x++;
            else origin.x--;
        }
        else
        {
            if (dir.y > 0) origin.y++;
            else origin.y--;
        }
    }

    static bool CompareGrid(ref Vector2Int roomOrigin, Vector2Int roomSize, Cell?[,] grid)
    {
        for (int i = -1; i < roomSize.x + 1; i++)
        {
            for (int j = -1; j < roomSize.y + 1; j++)
            {
                if (grid[roomOrigin.x + i, roomOrigin.y + j] != null)
                {
                    Debug.Log("Collision Detected");

                    SolveCollision(ref roomOrigin, roomSize, new Vector2Int(roomOrigin.x + i, roomOrigin.y + j), grid);

                    return true;
                }
            }
        }

        Debug.Log("Collision not detected");

        return false;
    }

    public static void CheckOverlap(ref Vector2Int roomOrigin, Vector2Int roomSize, Cell?[,] grid)
    {
        int safetyCounter = 0; //This shouldn't be done like this. Only to avoid infinite loops.

        while (CompareGrid(ref roomOrigin, roomSize, grid) == true && safetyCounter < 1000)
        {
            safetyCounter++;
        }
    }
}
