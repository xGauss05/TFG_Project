using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntranceDirection
{
    North,
    South,
    East,
    West
}
public struct Cell
{
    uint roomID;
    public NodeType type;
    public EntranceDirection? entrance;

    public Cell(uint roomID, NodeType type)
    {
        this.type = type;
        this.roomID = roomID;
        entrance = null;
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

    //Using a class and not a struct bc struct is a value type, not a
    //reference, so it can not be modified directly in the dictionary
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

    void GenerateLevel()
    {
        activeGraph = graphGenerator.activeGraph;

        //Generate layout positions
        Dictionary<uint, Vector3> positions = GenerateLayoutPositions();

        //Fill grid
        List<string> spawnerTags = new List<string> { "PlayerSpawnpoint", "BasicZombieSpawnpoint", "FastZombieSpawnpoint", "BossZombieSpawnpoint", "ZombieSpawnpoint" };
        List<Transform> spawners = new List<Transform>();
        List<Unity.Netcode.NetworkObject> objectsToSpawn = new List<Unity.Netcode.NetworkObject>();

        for (uint i = 0; i < activeGraph.nodes.Count; i++)
        {
            //Select the room you are going to place
            GameObject selectedRoomPrefab = (GameObject)normalRoom;

            if (activeGraph.nodes[i].type == NodeType.Start) selectedRoomPrefab = (GameObject)startRoom;
            else if (activeGraph.nodes[i].type == NodeType.Goal) selectedRoomPrefab = (GameObject)goalRoom;

            GeneratorRoom toSet = selectedRoomPrefab.GetComponent<GeneratorRoom>();

            Vector2Int posInGrid = WorldToGrid(positions[i]);

            //Check overlap?
            GeneratorCollisionSolver.CheckOverlap(ref posInGrid, toSet.size, grid);

            //Once generation is solved, place physical rooms
            for (int j = 0; j < toSet.size.x; j++)
            {
                for (int k = 0; k < toSet.size.y; k++)
                {
                    grid[posInGrid.x - j, posInGrid.y + k] = new Cell(i, activeGraph.nodes[i].type);
                }
            }

            GameObject roomInstance = Instantiate(selectedRoomPrefab, GridToWorld(posInGrid.x, posInGrid.y), Quaternion.identity);

            foreach (Transform child in roomInstance.transform)
            {
                if (spawnerTags.Contains(child.tag))
                {
                    spawners.Add(child);
                }

                if (!child.gameObject.isStatic)
                {
                    child.SetParent(null);

                    objectsToSpawn.Add(child.GetComponent<Unity.Netcode.NetworkObject>());
                    //Unity.Netcode.NetworkObject objToSpawn = child.GetComponent<Unity.Netcode.NetworkObject>();
                    //objToSpawn.Spawn();
                }
            }
        }

        OnLevelGenerationComplete?.Invoke(spawners, objectsToSpawn);
    }
}
