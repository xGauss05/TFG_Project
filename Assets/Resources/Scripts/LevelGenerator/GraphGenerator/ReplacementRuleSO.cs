using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Replacement Rule", menuName = "Rule")]
public class ReplacementRuleSO : ScriptableObject
{
    public TextAsset conditionGraph;
    public List<TextAsset> possibleTargetGraphs;
}