using System.Collections.Generic;
using UnityEngine;

public abstract class BaseNode : MonoBehaviour
{
    [SerializeField] List<BaseNode> _neighbors = new();
    public IReadOnlyList<BaseNode> Neighbors => _neighbors;

    public BaseNode Parent { get; set; }

    public Vector3 Position => transform.position;

    public virtual float MovementCost => 1f;

    public float GCost { get; set; } = float.MaxValue;
    public float HCost { get; set; } = 0f;
    public float FCost => GCost + HCost;


    public virtual void ResetPathFinding()
    {
        Parent = null;
        GCost = float.MaxValue;
        HCost = 0f;
    }
    public void AddNeighbor(BaseNode node)
    {
        if (!node || node == this) return;

        if (!_neighbors.Contains(node)) _neighbors.Add(node);
    }
    public void ClearNeighboirs() => _neighbors.Clear();
}