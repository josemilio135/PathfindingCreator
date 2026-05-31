using System.Collections.Generic;
using UnityEngine;

public class NavNode : MonoBehaviour, INode
{
    public INode Parent { get; set; }
    public IReadOnlyList<INode> Neighbors => _neighbors;
    [SerializeField] List<NavNode> _neighbors = new();

    public Vector3 Position => transform.position;

    public float MovementCost => 1f;

    public float GCost { get; set; } = float.MaxValue;
    public float HCost { get; set; } = 0f;
    public float FCost => GCost + HCost;


    public void ResetPathFinding()
    {
        GCost = float.MaxValue;
        HCost = 0f;
        Parent = null;
    }
    public void AddNeighbor(INode node)
    {
        if (node == null) return;
        if (node is not NavNode navNode) return;
        if (navNode == this) return;

        if (!_neighbors.Contains(navNode)) _neighbors.Add(navNode);
    }
    public void ClearNeighboirs()
    {
        _neighbors.Clear();
    }
}

