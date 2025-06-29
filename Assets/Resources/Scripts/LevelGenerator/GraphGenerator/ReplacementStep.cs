using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReplacementStep
{
    [SerializeField] List<ReplacementRuleSO> rulesToApply = new List<ReplacementRuleSO>();

    private Graph activeGraph;
    private Graph conditionGraph;
    private Graph targetGraph;

    public void ApplyRules(Graph activeGraph, int seed)
    {
        Random.InitState(seed);

        this.activeGraph = activeGraph;

        foreach (var rule in rulesToApply)
        {
            conditionGraph = new Graph(JsonUtility.FromJson<Graph_Data>(rule.conditionGraph.text));

            //This while caused problems in the case of recursive graph results
            //while (activeGraph.Contains(conditionGraph, out List<NodeRelation> nodeIdsList))
            if (activeGraph.Contains(conditionGraph, out List<NodeRelation> nodeIdsList))
            {
                //Here generate random number and select from list
                int targetGraphIndex = Random.Range(0, rule.possibleTargetGraphs.Count);

                targetGraph = new Graph(JsonUtility.FromJson<Graph_Data>(rule.possibleTargetGraphs[targetGraphIndex].text));

                ReplaceSubgraphInstance(nodeIdsList);
            }
        }
    }

    uint FindMainValueFromCondition(List<NodeRelation> relationsList, uint conditionValue)
    {
        if (relationsList.Count <= 0) { Debug.LogWarning("There are no relations in the list"); return 0; }

        foreach (var relation in relationsList)
        {
            if (relation.conditionID == conditionValue)
            {
                return relation.mainID;
            }
        }

        Debug.Log("Could not find relation between condition node " + conditionValue + " and any node in the main graph");
        return 0;
    }
    uint FindConditionValueFromMain(List<NodeRelation> relationsList, uint mainValue)
    {
        if (relationsList.Count <= 0) { Debug.LogWarning("There are no relations in the list"); return 0; }

        foreach (var relation in relationsList)
        {
            if (relation.mainID == mainValue)
            {
                return relation.conditionID;
            }
        }

        Debug.Log("Could not find relation between main node " + mainValue + " and any node in the condition graph");
        return 0;
    }

    void ReplaceSubgraphInstance(List<NodeRelation> nodesList)
    {
        //From the target graph we will need to add the nodes that are not existing in the active graph,
        //so we add them all to a list and we will remove each node that is contained in the main graph later.
        List<uint> nodesToAdd = new List<uint>();
        foreach (var node in targetGraph.nodes)
        {
            nodesToAdd.Add(node.Key);
        }

        //For each main graph node existing in the condition subgraph
        foreach (var relation in nodesList)
        {
            //Process Step 1: We don't need to assign values since that is already done in
            //the subgraph check process, we store these values in the "NodeRelations" list.

            //Process Step 2: Break all relations present in condition graph
            List<uint> neighborsToRemove = new List<uint>();
            foreach (var mainNeighbor in activeGraph.nodes[relation.mainID].neighbors)
            {
                if (conditionGraph.nodes[relation.conditionID].neighbors.Contains(FindConditionValueFromMain(nodesList, mainNeighbor)))
                {
                    Debug.Log("Removing connection from node " + relation.mainID + " to node " + mainNeighbor);
                    neighborsToRemove.Add(mainNeighbor);
                }
            }
            foreach (var neighbor in neighborsToRemove)
            {
                activeGraph.nodes[relation.mainID].neighbors.Remove(neighbor);
            }

            //Process Step 3: Convert to target types
            activeGraph.nodes[relation.mainID].type = targetGraph.nodes[relation.conditionID].type;

            nodesToAdd.Remove(relation.conditionID);
        }

        //Step 4: Add missing nodes
        foreach (var missingNode in nodesToAdd)
        {
            uint newNodeID = activeGraph.AddNode(targetGraph.nodes[missingNode].type);

            nodesList.Add(new NodeRelation(newNodeID, missingNode));
        }

        //Step 5: Create resulting connections
        foreach (var relation in nodesList)
        {
            foreach (var targetNeighbor in targetGraph.nodes[relation.conditionID].neighbors)
            {
                activeGraph.nodes[relation.mainID].neighbors.Add(FindMainValueFromCondition(nodesList, targetNeighbor));//relation value of targetNeighbor
            }
        }
    }
}
