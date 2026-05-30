using System.Collections.Generic;
using UnityEngine;

public class NavNode : MonoBehaviour, INode
{
    //  public NavNode Parent { get; set; }
    public IReadOnlyList<INode> Neighbors => _neighbors;
    readonly List<INode> _neighbors = new();

    public Vector3 Position => transform.position;

    public float MovementCost => 1f;

    public float GCost { get; set; } = float.MaxValue;
    public float HCost { get; set; } = 0f;
    public float FCost => GCost + HCost;


    public void ResetPathFinding()
    {
        GCost = float.MaxValue;
        HCost = 0f;
        //Parent = null;
    }
    public void AddNeighbor(NavNode node)
    {
        if (!node || node == this) return;
        if (!_neighbors.Contains(node)) _neighbors.Add(node);
    }
    public void ClearNeighboirs() => _neighbors.Clear();
}
