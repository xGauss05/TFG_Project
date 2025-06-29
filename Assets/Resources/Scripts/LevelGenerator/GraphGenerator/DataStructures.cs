using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Node_Data
{
    public uint id;
    public NodeType type;  // The type of the node
    public List<uint> neighbors;
}

[System.Serializable]
public class Graph_Data
{
    public List<Node_Data> nodes;
}