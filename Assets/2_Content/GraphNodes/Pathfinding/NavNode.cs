using System.Collections.Generic;
using UnityEngine;

public class NavNode : MonoBehaviour, INode
{
    public INode Parent { get; set; }
    public IReadOnlyList<INode> Neighbors => _neighbors;
    readonly List<INode> _neighbors = new();
    [SerializeField] List<NavNode> debugNeighbors = new();

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
        if (node is NavNode navNode && navNode == this) return;

        if (!_neighbors.Contains(node)) _neighbors.Add(node);

        if (node is NavNode nav) debugNeighbors.Add(nav);
    }
    public void ClearNeighboirs()
    {
        _neighbors.Clear();
        debugNeighbors.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        foreach (var neighbor in debugNeighbors)
        {
            if (neighbor == null) continue;
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}
