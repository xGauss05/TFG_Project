using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

public struct Cell
{
    public NodeType type;
    public Entrance? entrance;
    public Direction? direction;

    public Cell(NodeType type)
    {
        this.type = type;
        entrance = null;
        direction = null;
    }

    public Cell(NodeType type, Direction direction)
    {
        this.type = type;
        entrance = null;
        this.direction = direction;
    }
}

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GraphGenerator graphGenerator;
    Graph activeGraph;

    public int gridSize = 100; //200 total, 100 positive 100 negative. 40 cells a row, 10 min size rooms max
    public Cell?[,] grid;

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

    public System.Action<List<Transform>, List<Unity.Netcode.NetworkObject>> OnLevelGenerationComplete;


    private void Awake()
    {
        graphGenerator.OnGraphComplete.AddListener(GenerateLevel);
        grid = new Cell?[gridSize * 2, gridSize * 2];
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 gridAlignedPosition = new Vector3(Mathf.Round(worldPosition.x / 5.0f) * 5,
                                                  Mathf.Round(worldPosition.y / 5.0f) * 5,
                                                  Mathf.Round(worldPosition.z / 5.0f) * 5); //5 is the size of each cell

        //Negative world values will start at index 0, and positive world values will start at index gridSize
        return new Vector2Int(((int)gridAlignedPosition.x / 5) + gridSize, ((int)gridAlignedPosition.z / 5) + gridSize);
    }
    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3((x - gridSize) * 5f, 0, (y - gridSize) * 5f);
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
    List<GeneratorRoom> PlaceRooms(Dictionary<uint, Vector3> positions, List<Transform> spawners, List<Unity.Netcode.NetworkObject> objectsToSpawn)
    {
        List<string> spawnerTags = new List<string> { "PlayerSpawnpoint", "BasicZombieSpawnpoint", "FastZombieSpawnpoint", "BossZombieSpawnpoint", "ZombieSpawnpoint" };

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

            //Check overlap?
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

            List<Transform> objsToUnparent = new List<Transform>();
            foreach (Transform child in roomInstance.transform)
            {
                if (spawnerTags.Contains(child.tag))
                {
                    spawners.Add(child);
                }

                Debug.Log(child.name);

                if (!child.gameObject.isStatic)
                {
                    objsToUnparent.Add(child);
                }
            }

            foreach (var GO in objsToUnparent)
            {
                GO.SetParent(null);

                //objectsToSpawn.Add(child.GetComponent<Unity.Netcode.NetworkObject>());
                Unity.Netcode.NetworkObject objToSpawn = GO.GetComponent<Unity.Netcode.NetworkObject>();
                objToSpawn.Spawn();
            }
        }

        return placedRooms;
    }

    Dictionary<Direction, Vector2Int> directionCoords = new Dictionary<Direction, Vector2Int>
                {
                    { Direction.North,  Vector2Int.up},
                    { Direction.South,  Vector2Int.down},
                    { Direction.East,   Vector2Int.right},
                    { Direction.West,   Vector2Int.left}
                };
    short[,] MakeTilesFromGrid()
    {
        short[,] returnTileMap = new short[gridSize * 2, gridSize * 2];

        for (int i = 0; i < gridSize * 2; i++)
        {
            for (int j = 0; j < gridSize * 2; j++)
            {
                if (grid[i, j] != null && grid[i, j].Value.type == NodeType.Corridor)
                {
                    returnTileMap[i, j] = 50; //Try to find a balanced value
                }
                else if (grid[i, j] != null)
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
    Object GetCorridorShape(Vector2Int currentPosition, Vector2Int previousPosition, out Vector3 positionAdjustment, out Quaternion rotationAdjustment, Direction? firstCellDirection = null)
    {
        Direction? currentDirection = grid[currentPosition.x, currentPosition.y].Value.direction;
        Direction? previousDirection = grid[previousPosition.x, previousPosition.y].Value.direction;

        if (firstCellDirection != null)
            previousDirection = firstCellDirection;

        //Straight path
        if (currentDirection == previousDirection)
        {
            //Vertical
            if (currentDirection == Direction.North || currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.left * 5;
                rotationAdjustment = Quaternion.Euler(0, 90, 0);
            }
            //Horizontal
            else
            {
                positionAdjustment = Vector3.zero;
                rotationAdjustment = Quaternion.identity;
            }
            return straightCorridor;
        }
        //Shoulder path
        else
        {
            //Up-right
            if (previousDirection == Direction.North && currentDirection == Direction.East ||
                previousDirection == Direction.West && currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.left * 5;
                rotationAdjustment = Quaternion.Euler(0, 90, 0);
            }
            //Down-left
            else if (previousDirection == Direction.South && currentDirection == Direction.West ||
                     previousDirection == Direction.East && currentDirection == Direction.North)
            {
                positionAdjustment = Vector3.forward * 5;
                rotationAdjustment = Quaternion.Euler(0, -90, 0);
            }
            //Up-left
            else if (previousDirection == Direction.North && currentDirection == Direction.West ||
                     previousDirection == Direction.East && currentDirection == Direction.South)
            {
                positionAdjustment = Vector3.forward * 5 + Vector3.left * 5;
                rotationAdjustment = Quaternion.Euler(0, 180, 0);
            }
            //Down-right
            else
            {
                positionAdjustment = Vector3.zero;
                rotationAdjustment = Quaternion.identity;
            }
            return shoulderCorridor;
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
                short[,] tiles = MakeTilesFromGrid();

                var pathfinderOptions = new AStar.Options.PathFinderOptions
                {
                    PunishChangeDirection = true,
                    UseDiagonals = false,
                    Weighting = AStar.Options.Weighting.Negative,
                };

                var worldGrid = new WorldGrid(tiles);
                var pathfinder = new PathFinder(worldGrid, pathfinderOptions);

                Vector2Int pathFindingStart = selectedStartPos + directionCoords[selectedEntranceStart.direction];
                Vector2Int pathFindingEnd = selectedEndPos + directionCoords[selectedEntranceEnd.direction];

                //Pathfinding execution
                Position[] path = pathfinder.FindPath(new Position(pathFindingStart.x, pathFindingStart.y),
                                                      new Position(pathFindingEnd.x, pathFindingEnd.y));
                if (path.Length <= 0)
                {
                    Debug.LogError("Could not find suitable path");
                };

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
                        corridorToInstantiate = GetCorridorShape(currentPos, currentPos, out posToAdd, out rotation, selectedEntranceStart.direction);
                    }
                    else
                    {
                        corridorToInstantiate = GetCorridorShape(currentPos, new Vector2Int(path[k - 1].Row, path[k - 1].Column), out posToAdd, out rotation);
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

        List<Transform> spawners = new List<Transform>();
        List<Unity.Netcode.NetworkObject> objectsToSpawn = new List<Unity.Netcode.NetworkObject>();

        List<GeneratorRoom> placedRooms = PlaceRooms(positions, spawners, objectsToSpawn);

        PlaceCorridors(placedRooms, positions);

        GetComponent<Unity.AI.Navigation.NavMeshSurface>().BuildNavMesh();

        OnLevelGenerationComplete?.Invoke(spawners, objectsToSpawn);
    }
}
