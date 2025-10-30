using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

public struct Cell
{
    public NodeType type;
    public Direction? direction;

    public Cell(NodeType type)
    {
        this.type = type;
        direction = null;
    }

    public Cell(NodeType type, Direction direction)
    {
        this.type = type;
        this.direction = direction;
    }
}

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GraphGenerator graphGenerator;
    Graph activeGraph;

    public int gridSize = 100; //200 total, 100 positive 100 negative. 40 cells a row, 10 min size rooms max
    public int cellSize = 10; //5x5 meters/units per cell
    public Cell?[,] grid;
    public Cell?[,] subwayGrid;

    //This should ideally be changed to lists of prefabs or a better way to initialize them
    [Space]
    [SerializeField] Object startRoom;
    [SerializeField] Object goalRoom;
    [SerializeField] Object normalRoom;
    [SerializeField] Object lockRoom;
    [SerializeField] Object keyRoom;
    [SerializeField] Object collapsingBridgeRoom;
    [SerializeField] Object oneWayDropRoom;
    [SerializeField] Object straightCorridor;
    [SerializeField] Object shoulderCorridor;
    [SerializeField] Object undergroundEntrance;
    [SerializeField] Object straightUnderground;
    [SerializeField] Object shoulderUnderground;

    public System.Action OnLevelGenerationComplete;


    private void Awake()
    {
        grid = new Cell?[gridSize * 2, gridSize * 2];
        subwayGrid = new Cell?[gridSize * 2, gridSize * 2];

        graphGenerator.OnGraphComplete.AddListener(GenerateLevel);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 gridAlignedPosition = new Vector3(Mathf.Round(worldPosition.x / cellSize) * cellSize,
                                                  Mathf.Round(worldPosition.y / cellSize) * cellSize,
                                                  Mathf.Round(worldPosition.z / cellSize) * cellSize);

        //Negative world values will start at index 0, and positive world values will start at index gridSize
        return new Vector2Int(((int)gridAlignedPosition.x / (int)cellSize) + gridSize, ((int)gridAlignedPosition.z / (int)cellSize) + gridSize);
    }
    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3((x - gridSize) * cellSize, 0, (y - gridSize) * cellSize);
    }

    Vector2Int GenerateRandomPosition()
    {
        Vector3 pos = new Vector3(Random.Range(-gridSize, gridSize), 0, Random.Range(-gridSize, gridSize));

        return WorldToGrid(pos);
    }

    //Using a class and not a struct bc struct is a value type, not a reference, so it can not be modified directly in the dictionary
    class NodePositioner
    {
        public Vector2 position;
        public Vector2 velocity;
        public NodePositioner(Vector2 position, Vector2 velocity)
        {
            this.position = position;
            this.velocity = velocity;
        }
    }
    void SolvePositions(Dictionary<uint, NodePositioner> nodes)
    {
        int iterations = 50000;
        float repulsionStrength = 5000f;
        float springLength = 2f;
        float springStrength = 0.1f;
        float damping = 0.9f;

        float firstTime = Time.realtimeSinceStartup;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Apply repulsion between all pairs of nodes
            for (uint i = 0; i < nodes.Count; i++)
            {
                Vector2 force = Vector2.zero;
                for (uint j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    Vector2 diff = nodes[i].position - nodes[j].position;
                    float dist = Mathf.Max(diff.magnitude, 0.01f);
                    force += diff.normalized * (repulsionStrength / (dist * dist));
                }
                nodes[i].velocity += force;
            }

            // Apply spring force for each edge
            foreach (var edge in activeGraph.edges)
            {
                NodePositioner a = nodes[edge.from];
                NodePositioner b = nodes[edge.to];
                Vector2 delta = b.position - a.position;
                float dist = delta.magnitude;
                Vector2 springForce = delta.normalized * (dist - springLength) * springStrength;
                a.velocity += springForce;
                b.velocity -= springForce;
            }

            // Update positions and apply damping
            foreach (var node in nodes)
            {
                node.Value.position += node.Value.velocity;
                node.Value.velocity *= damping;
            }
        }

        float secondTime = Time.realtimeSinceStartup;

        Debug.Log($"Solver elapsed time: {secondTime - firstTime}");
    }
    Dictionary<uint, Vector3> GenerateLayoutPositions()
    {
        Dictionary<uint, NodePositioner> nodes = new Dictionary<uint, NodePositioner>();
        for (uint i = 0; i < activeGraph.nodes.Count; i++)
        {
            nodes.Add(i, new NodePositioner(GenerateRandomPosition(), Vector2.zero));
        }

        SolvePositions(nodes);

        Dictionary<uint, Vector3> positions = new Dictionary<uint, Vector3>();
        for (uint i = 0; i < nodes.Count; i++)
        {
            Vector2 pos = nodes[i].position;
            positions.Add(i, new Vector3(pos.x, 0, pos.y));
        }

        return positions;
    }
    List<GeneratorRoom> PlaceRooms(Dictionary<uint, Vector3> positions)
    {
        List<GeneratorRoom> placedRooms = new List<GeneratorRoom>();

        for (uint i = 0; i < activeGraph.nodes.Count; i++)
        {
            //Select the room you are going to place
            GameObject selectedRoomPrefab;

            switch (activeGraph.nodes[i].type)
            {
                case NodeType.Start:            selectedRoomPrefab = (GameObject)startRoom; break;
                case NodeType.Goal:             selectedRoomPrefab = (GameObject)goalRoom; break;
                case NodeType.Room:             selectedRoomPrefab = (GameObject)normalRoom; break;
                case NodeType.Lock:             selectedRoomPrefab = (GameObject)lockRoom; break;
                case NodeType.Key:              selectedRoomPrefab = (GameObject)keyRoom; break;
                case NodeType.CollapsingBridge: selectedRoomPrefab = (GameObject)collapsingBridgeRoom; break;
                case NodeType.OneWayDrop:       selectedRoomPrefab = (GameObject)oneWayDropRoom; break;
                default:                        selectedRoomPrefab = (GameObject)normalRoom; break;
            }

            GeneratorRoom toSet = selectedRoomPrefab.GetComponent<GeneratorRoom>();

            Vector2Int posInGrid = WorldToGrid(positions[i]);

            //Check overlap
            GeneratorCollisionSolver.CheckOverlap(ref posInGrid, toSet.size, grid);

            //Once generation is solved, place physical rooms
            for (int j = 0; j < toSet.size.x; j++)
            {
                for (int k = 0; k < toSet.size.y; k++)
                {
                    grid[posInGrid.x - j, posInGrid.y + k] = new Cell(activeGraph.nodes[i].type);
                }
            }

            GameObject roomInstance = Instantiate(selectedRoomPrefab, GridToWorld(posInGrid.x, posInGrid.y), Quaternion.identity);
            placedRooms.Add(roomInstance.GetComponent<GeneratorRoom>());
        }

        return placedRooms;
    }

    Vector2Int GetDirectionCoords(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:   return Vector2Int.up;
            case Direction.East:    return Vector2Int.right;
            case Direction.South:   return Vector2Int.down;
            case Direction.West:    return Vector2Int.left;
            default:                return Vector2Int.zero;
        }
    }
    short[,] MakeTilesFromGrid(Cell?[,] gridToParse)
    {
        short[,] returnTileMap = new short[gridSize * 2, gridSize * 2];

        for (int i = 0; i < gridSize * 2; i++)
        {
            for (int j = 0; j < gridSize * 2; j++)
            {
                if (gridToParse[i, j] != null && gridToParse[i, j].Value.type == NodeType.Corridor)
                {
                    returnTileMap[i, j] = 50; //Try to find a balanced value
                }
                else if (gridToParse[i, j] != null)
                {
                    returnTileMap[i, j] = 0;
                }
                else
                {
                    returnTileMap[i, j] = 1;
                }
            }
        }

        return returnTileMap;
    }
    Direction GetPathCellDirection(Vector2Int cellDir)
    {
        if (cellDir == Vector2Int.up)
            return Direction.North;
        else if (cellDir == Vector2Int.down)
            return Direction.South;
        else if (cellDir == Vector2Int.right)
            return Direction.East;
        else if (cellDir == Vector2Int.left)
            return Direction.West;
        else
            return new Direction();
    }
    //"Adjustment" parameters are variables to take into account and adjust the model based on the pivot of the object
    Object GetCorridorShape(Cell?[,] gridToCheck, Vector2Int currentPosition, Vector2Int previousPosition, out Vector3 positionAdjustment, out Quaternion rotationAdjustment, Direction? firstCellDirection = null)
    {
        Direction? currentDirection = gridToCheck[currentPosition.x, currentPosition.y].Value.direction;
        Direction? previousDirection = gridToCheck[previousPosition.x, previousPosition.y].Value.direction;

        if (firstCellDirection != null)
            previousDirection = firstCellDirection;

        //Straight path
        if (currentDirection == previousDirection)
        {
            //Vertical
            if (currentDirection == Direction.North || currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.left * cellSize;
                rotationAdjustment = Quaternion.Euler(0, 90, 0);
            }
            //Horizontal
            else
            {
                positionAdjustment = Vector3.zero;
                rotationAdjustment = Quaternion.identity;
            }

            return (gridToCheck == grid) ? straightCorridor : straightUnderground;
        }
        //Shoulder path
        else
        {
            //Up-right
            if (previousDirection == Direction.North && currentDirection == Direction.East ||
                previousDirection == Direction.West && currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.left * cellSize;
                rotationAdjustment = Quaternion.Euler(0, 90, 0);
            }
            //Down-left
            else if (previousDirection == Direction.South && currentDirection == Direction.West ||
                     previousDirection == Direction.East && currentDirection == Direction.North)
            {
                positionAdjustment = Vector3.forward * cellSize;
                rotationAdjustment = Quaternion.Euler(0, -90, 0);
            }
            //Up-left
            else if (previousDirection == Direction.North && currentDirection == Direction.West ||
                     previousDirection == Direction.East && currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.forward * cellSize + Vector3.left * cellSize;
                rotationAdjustment = Quaternion.Euler(0, 180, 0);
            }
            //Down-right
            else
            {
                positionAdjustment = Vector3.zero;
                rotationAdjustment = Quaternion.identity;
            }
            
            return (gridToCheck == grid) ? shoulderCorridor : shoulderUnderground;
        }
    }
    struct SubwayInfo
    {
        public Vector2Int pathfindingCellDelta; //what cell does the pathfinding use with respect to the entrance position
        public Vector2Int gridFillerPivot; //where does the space the subway occupies on the grid start (world coords)
        public Vector2Int positionDelta; //how much you must move from entrance to snap to grid after rotating
        public float rotation;
        public bool horizontal;

        public SubwayInfo(Vector2Int pathfindingCellDelta, Vector2Int gridFillerPivot, Vector2Int positionDelta, float rotation, bool horizontal)
        {
            this.pathfindingCellDelta = pathfindingCellDelta;
            this.gridFillerPivot = gridFillerPivot;
            this.positionDelta = positionDelta;
            this.rotation = rotation;
            this.horizontal = horizontal;
        }
    }
    SubwayInfo GetSubwayInfo(Direction entranceDirection, Vector2Int entrancePos)
    {
        switch (entranceDirection)
        {
            case Direction.North: return new SubwayInfo(pathfindingCellDelta: new Vector2Int(-1, -1), 
                                                        gridFillerPivot: entrancePos + new Vector2Int(-1, 0), 
                                                        positionDelta: Vector2Int.up, 
                                                        horizontal: true,
                                                        rotation: 0);

            case Direction.South: return new SubwayInfo(pathfindingCellDelta: new Vector2Int(3, 2),
                                                        gridFillerPivot: entrancePos + new Vector2Int(-2, 0),
                                                        positionDelta: Vector2Int.left,
                                                        horizontal: true,
                                                        rotation: 180);

            case Direction.East: return new SubwayInfo(pathfindingCellDelta: new Vector2Int(-1, 1),
                                                       gridFillerPivot: entrancePos + new Vector2Int(0, 0),
                                                       positionDelta: Vector2Int.zero,
                                                       horizontal: false,
                                                       rotation: 90);

            case Direction.West: return new SubwayInfo(pathfindingCellDelta: new Vector2Int(1, -1),
                                                       gridFillerPivot: entrancePos + new Vector2Int(-1, -1),
                                                       positionDelta: new Vector2Int(-1, 1),
                                                       horizontal: false,
                                                       rotation: -90);
            default: return new SubwayInfo();
        }
    }
    void PlaceTunnel(Entrance startingEntrance, Entrance endingEntrance, Vector2Int startEntrancePos, Vector2Int endEntrancePos)
    {
        //Starting Room
        SubwayInfo startInfo = GetSubwayInfo(startingEntrance.direction, startEntrancePos);

        Vector2Int startGlobalPos = startEntrancePos + startInfo.positionDelta;
        Instantiate(undergroundEntrance, GridToWorld(startGlobalPos.x, startGlobalPos.y), Quaternion.Euler(0, startInfo.rotation, 0));

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                subwayGrid[startInfo.gridFillerPivot.x + i, startInfo.gridFillerPivot.y + j] = new Cell(NodeType.Room);
            }
        }

        //Ending room
        SubwayInfo endInfo = GetSubwayInfo(endingEntrance.direction, endEntrancePos);

        Vector2Int endGlobalPos = endEntrancePos + endInfo.positionDelta;
        Instantiate(undergroundEntrance, GridToWorld(endGlobalPos.x, endGlobalPos.y), Quaternion.Euler(0, endInfo.rotation, 0));

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                subwayGrid[endInfo.gridFillerPivot.x + i, endInfo.gridFillerPivot.y + j] = new Cell(NodeType.Room);
            }
        }

        short[,] tiles = MakeTilesFromGrid(subwayGrid);

        //Pathfinding is startEntrancePos + pathfindingStartingPosDelta
        var pathfinderOptions = new AStar.Options.PathFinderOptions
        {
            PunishChangeDirection = true,
            UseDiagonals = false,
            Weighting = AStar.Options.Weighting.Negative,
        };

        var worldGrid = new WorldGrid(tiles);
        var pathfinder = new PathFinder(worldGrid, pathfinderOptions);

        Vector2Int pathFindingStart = startEntrancePos + startInfo.pathfindingCellDelta;
        Vector2Int pathFindingEnd = endEntrancePos + endInfo.pathfindingCellDelta;

        //Pathfinding execution
        Position[] path = pathfinder.FindPath(new Position(pathFindingStart.x, pathFindingStart.y),
                                              new Position(pathFindingEnd.x, pathFindingEnd.y));

        if (path.Length <= 0)
        {
            Debug.LogWarning("Could not find suitable path");
        };

        //Setting individual path cells
        for (int k = 0; k < path.Length; k++)
        {
            //Fill grid
            Vector2Int currentPos = new Vector2Int(path[k].Row, path[k].Column);

            if (k == path.Length - 1)
            {
                Vector2Int correction;
                switch (endingEntrance.direction)
                {
                    case Direction.North:   correction = Vector2Int.up; break;
                    case Direction.East:    correction = Vector2Int.right; break;
                    case Direction.South:   correction = Vector2Int.down; break;
                    case Direction.West:    correction = Vector2Int.left; break;
                    default:                correction = Vector2Int.zero; break;
                }

                Vector2Int dir = (pathFindingEnd + correction) - currentPos;
                subwayGrid[currentPos.x, currentPos.y] = new Cell(NodeType.Corridor, GetPathCellDirection(dir));
            }
            else
            {
                Vector2Int dir = new Vector2Int(path[k + 1].Row - currentPos.x, path[k + 1].Column - currentPos.y);
                subwayGrid[currentPos.x, currentPos.y] = new Cell(NodeType.Corridor, GetPathCellDirection(dir));
            }

            //Instantiate
            Vector3 posToAdd;
            Quaternion rotation;
            Object corridorToInstantiate;

            if (k == 0)
            {
                Direction startDir = (Direction)(((int)startingEntrance.direction + 2) % 4); //Flip direction

                corridorToInstantiate = GetCorridorShape(subwayGrid, currentPos, currentPos, out posToAdd, out rotation, startDir);
            }
            else
            {
                corridorToInstantiate = GetCorridorShape(subwayGrid, currentPos, new Vector2Int(path[k - 1].Row, path[k - 1].Column), out posToAdd, out rotation);
            }

            Instantiate(corridorToInstantiate, GridToWorld(currentPos.x, currentPos.y) + posToAdd + Vector3.down * 8.0f, rotation);
        }
    }
    void PlaceCorridors(List<GeneratorRoom> placedRooms, Dictionary<uint, Vector3> positions)
    {
        for (uint i = 0; i < activeGraph.nodes.Count; i++)
        {
            for (int j = 0; j < activeGraph.nodes[i].neighbors.Count; j++)
            {
                //Find closest entrances
                GeneratorRoom startRoom = placedRooms[(int)i];
                GeneratorRoom endRoom = placedRooms[(int)activeGraph.nodes[i].neighbors[j]];

                float lowestDistance = float.MaxValue;
                Entrance selectedEntranceStart = startRoom.entrances[0];
                Entrance selectedEntranceEnd = endRoom.entrances[0];
                Vector2Int selectedStartPos = WorldToGrid(positions[i]);
                Vector2Int selectedEndPos = WorldToGrid(positions[activeGraph.nodes[i].neighbors[j]]);

                foreach (var startEntrance in startRoom.entrances)
                {
                    if (startEntrance.unlocked) continue;

                    foreach (var endEntrance in endRoom.entrances)
                    {
                        if (endEntrance.unlocked) continue;

                        Vector2Int currentStartPos = WorldToGrid(positions[i]) + startEntrance.localPosition;
                        Vector2Int currentEndPos = WorldToGrid(positions[activeGraph.nodes[i].neighbors[j]]) + endEntrance.localPosition;

                        float currentDistance = Vector2.Distance(currentStartPos, currentEndPos);
                        if (currentDistance < lowestDistance)
                        {
                            lowestDistance = currentDistance;

                            selectedEntranceStart = startEntrance;
                            selectedEntranceEnd = endEntrance;

                            selectedStartPos = currentStartPos;
                            selectedEndPos = currentEndPos;
                        }
                    }
                }

                selectedEntranceStart.unlocked = true;
                selectedEntranceEnd.unlocked = true;
                Destroy(selectedEntranceStart.blockage);
                Destroy(selectedEntranceEnd.blockage);

                //Pathfinding Set Up
                short[,] tiles = MakeTilesFromGrid(grid);

                var pathfinderOptions = new AStar.Options.PathFinderOptions
                {
                    PunishChangeDirection = true,
                    UseDiagonals = false,
                    Weighting = AStar.Options.Weighting.Negative,
                };

                var worldGrid = new WorldGrid(tiles);
                var pathfinder = new PathFinder(worldGrid, pathfinderOptions);

                Vector2Int pathFindingStart = selectedStartPos + GetDirectionCoords(selectedEntranceStart.direction);
                Vector2Int pathFindingEnd = selectedEndPos + GetDirectionCoords(selectedEntranceEnd.direction);

                //Pathfinding execution
                Position[] path = pathfinder.FindPath(new Position(pathFindingStart.x, pathFindingStart.y),
                                                      new Position(pathFindingEnd.x, pathFindingEnd.y));

                if (path.Length <= 0)
                {
                    Debug.LogWarning("Could not find suitable path");
                };

                //Check if underground
                bool underground = false;
                foreach (var item in path)
                {
                    if (tiles[item.Row, item.Column] == 50)
                    {
                        Debug.Log("Corridor must be underground");
                        PlaceTunnel(selectedEntranceStart, selectedEntranceEnd, selectedStartPos, selectedEndPos);
                        underground = true;
                    }
                }
                if (underground) continue;

                //Setting individual path cells
                for (int k = 0; k < path.Length; k++)
                {
                    //Fill grid
                    Vector2Int currentPos = new Vector2Int(path[k].Row, path[k].Column);

                    if (k == path.Length - 1)
                    {
                        Vector2Int dir = selectedEndPos - currentPos;
                        grid[currentPos.x, currentPos.y] = new Cell(NodeType.Corridor, GetPathCellDirection(dir));
                    }
                    else
                    {
                        Vector2Int dir = new Vector2Int(path[k + 1].Row - currentPos.x, path[k + 1].Column - currentPos.y);
                        grid[currentPos.x, currentPos.y] = new Cell(NodeType.Corridor, GetPathCellDirection(dir));
                    }

                    //Instantiate
                    Vector3 posToAdd;
                    Quaternion rotation;
                    Object corridorToInstantiate;

                    if (k == 0)
                    {
                        corridorToInstantiate = GetCorridorShape(grid, currentPos, currentPos, out posToAdd, out rotation, selectedEntranceStart.direction);
                    }
                    else
                    {
                        corridorToInstantiate = GetCorridorShape(grid, currentPos, new Vector2Int(path[k - 1].Row, path[k - 1].Column), out posToAdd, out rotation);
                    }

                    Instantiate(corridorToInstantiate, GridToWorld(currentPos.x, currentPos.y) + posToAdd, rotation);
                }
            }
        }
    }

    void GenerateLevel()
    {
        activeGraph = graphGenerator.activeGraph;

        //Generate layout positions
        Dictionary<uint, Vector3> positions = GenerateLayoutPositions();

        List<GeneratorRoom> placedRooms = PlaceRooms(positions);

        PlaceCorridors(placedRooms, positions);

        if (LevelManager.Singleton.IsHost)
        {
            GetComponent<Unity.AI.Navigation.NavMeshSurface>().BuildNavMesh();
            StartCoroutine(RegenerateNavmesh());
        }

        OnLevelGenerationComplete?.Invoke();
    }

    IEnumerator RegenerateNavmesh()
    {
        yield return new WaitForSeconds(3);

        GetComponent<Unity.AI.Navigation.NavMeshSurface>().BuildNavMesh();
    }
}
