using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Start,
    Goal,
    Room,
    Lock,
    Key,
    CollapsingBridge,
    OneWayDrop
}

public struct NodeRelation
{
    public NodeRelation(uint mainID, uint conditionID)
    {
        this.mainID = mainID;
        this.conditionID = conditionID;
    }

    public uint mainID;
    public uint conditionID;
}

public class Node
{
    public NodeType type;  // The type of the node
    public List<uint> neighbors;

    public Node(NodeType nodeType)
    {
        type = nodeType;
        neighbors = new List<uint>();
    }
    public Node(Node_Data data)
    {
        type = data.type;

        neighbors = new List<uint>(data.neighbors);
    }
}

public class Graph
{
    //int represents Node id
    public Dictionary<uint, Node> nodes { get; private set; }

    public struct Edge
    {
        public uint from { get; private set; }
        public uint to { get; private set; }

        public Edge(uint from, uint to)
        {
            this.from = from;
            this.to = to;
        }
    }
    public List<Edge> edges
    {
        get
        {
            List<Edge> returnList = new List<Edge>();

            foreach (var node in nodes)
            {
                if (node.Value.neighbors.Count <= 0) { continue; }

                foreach (var neighbor in node.Value.neighbors)
                {
                    returnList.Add(new Edge(node.Key, neighbor));
                }
            }

            return returnList;
        }
    }

    public Graph()
    {
        nodes = new Dictionary<uint, Node>();
    }
    public Graph(Graph_Data data)
    {
        nodes = new Dictionary<uint, Node>();
        foreach (var nodeData in data.nodes)
        {
            nodes.Add(nodeData.id, new Node(nodeData));
        }
    }

    public uint AddNode(NodeType type)
    {
        Node node = new Node(type);
        uint assignedID = GetLowestFreeID();

        nodes.Add(assignedID, node);
        return assignedID;
    }

    public void AddEdge(uint from, uint to)
    {
        if (!nodes[from].neighbors.Contains(to))
            nodes[from].neighbors.Add(to);
    }

    public bool Contains(Graph subgraph, out List<NodeRelation> mainGraphNodeRelations)
    {
        List<uint> findResult = FindIsomorphism(subgraph);

        mainGraphNodeRelations = new List<NodeRelation>();

        if (findResult == null || findResult.Count <= 0)
        {
            Debug.LogWarning("Could not find matching subgraph");
            return false;
        }

        for (int i = 0; i < findResult.Count; i++)
        {
            //Value is main graph, index is subgraph
            mainGraphNodeRelations.Add(new NodeRelation(findResult[i], (uint)i));
        }

        return true;
    }

    private uint GetLowestFreeID()
    {
        uint currentId = 0;
        while (nodes.ContainsKey(currentId))
        {
            currentId++;
        }
        return currentId;
    }

    //each index in the list represents a node in the subgraph, and the set is the possible
    //nodes in the main graph it could potentially be mapped to, which are to be pruned
    private void UpdatePossibleAssignments(Graph subgraph, List<HashSet<uint>> possibleAssignments)
    {
        bool anyChanges = true;

        while (anyChanges)
        {
            anyChanges = false;
            foreach (var subNode in subgraph.nodes) //i
            {
                if (subNode.Key >= possibleAssignments.Count) { continue; }

                List<uint> assignmentsToRemove = new List<uint>();
                foreach (var possibleMaingraphNode in possibleAssignments[(int)subNode.Key]) //j
                {
                    foreach (var subnodeNeighbor in subNode.Value.neighbors) //x
                    {
                        bool match = false;
                        foreach (var mainNode in nodes) //y
                        {
                            if (subnodeNeighbor >= possibleAssignments.Count || possibleMaingraphNode >= nodes.Count) { continue; }

                            if (possibleAssignments[(int)subnodeNeighbor].Contains(mainNode.Key) && nodes[possibleMaingraphNode].neighbors.Contains(mainNode.Key))
                            {
                                match = true;
                            }
                        }
                        if (!match)
                        {
                            //Removing directly while iterating over it will throw an exception
                            //possible_assignments[(int)subNode.Key].Remove(possible_maingraph_node);
                            assignmentsToRemove.Add(possibleMaingraphNode);
                            anyChanges = true;
                        }
                    }
                }
                foreach (var assignment in assignmentsToRemove)
                {
                    possibleAssignments[(int)subNode.Key].Remove(assignment);
                }
            }
        }
    }

    private bool Search(Graph subgraph, List<uint> assignments, List<HashSet<uint>> possibleAssignments)
    {
        UpdatePossibleAssignments(subgraph, possibleAssignments);

        int assignmentsCount = assignments.Count;

        //Make sure that every edge between assigned vertices in the subgraph is also an edge in the graph.
        foreach (var edge in subgraph.edges)
        {
            if (edge.from < assignmentsCount && edge.to < assignmentsCount)
            {
                uint mappedFrom = assignments[(int)edge.from];
                uint mappedTo = assignments[(int)edge.to];

                if (!edges.Contains(new Edge(mappedFrom, mappedTo)))
                    return false;
            }
        }

        //If all the vertices in the subgraph are assigned, then we are done.
        if (subgraph.nodes.Count == assignmentsCount)
        {
            return true;
        }

        //Fix for removing during iteration
        List<uint> iteratingAssignments = new List<uint>(possibleAssignments[assignmentsCount]);

        foreach (var assignment in iteratingAssignments)
        {
            if (!assignments.Contains(assignment))
            {
                assignments.Add(assignment);

                //Create a new set of possible assignments, where graph node j ("assignment")
                //is the only possibility for the assignment of subgraph node i ("assignmentsCount").

                //Make possibleAssignments deep copy
                List<HashSet<uint>> newPossibleAssignments = new List<HashSet<uint>>();
                foreach (var entry in possibleAssignments)
                {
                    newPossibleAssignments.Add(new HashSet<uint>(entry));
                }

                newPossibleAssignments[assignmentsCount] = new HashSet<uint> { assignment };

                if (Search(subgraph, assignments, newPossibleAssignments))
                {
                    return true;
                }

                assignments.RemoveAt(assignments.Count - 1);
            }
            possibleAssignments[assignmentsCount].Remove(assignment);
            UpdatePossibleAssignments(subgraph, possibleAssignments);
        }

        return false;
    }

    //This implementation is an adapted version of the one in this thread: https://stackoverflow.com/questions/13537716/how-to-partially-compare-two-graphs/13537776#13537776
    private List<uint> FindIsomorphism(Graph subgraph)
    {
        List<uint> assignments = new List<uint>();

        List<HashSet<uint>> possibleAssignments = new List<HashSet<uint>>();
        foreach (var subNode in subgraph.nodes)
        {
            HashSet<uint> setToAdd = new HashSet<uint>();

            foreach (var mainNode in nodes)
            {
                if (mainNode.Value.type == subNode.Value.type)
                {
                    setToAdd.Add(mainNode.Key);
                }
            }

            possibleAssignments.Add(setToAdd);
        }

        if (Search(subgraph, assignments, possibleAssignments))
        {
            return assignments;
        }

        return null;
    }
}