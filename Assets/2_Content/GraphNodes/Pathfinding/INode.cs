using System.Collections.Generic;
using UnityEngine;

public interface INode
{
    INode Parent { get; set; }
    IReadOnlyList<INode> Neighbors { get; }

    Vector3 Position { get; }

    float MovementCost { get; }

    public float GCost { get; set; }
    public float HCost { get; set; }
    public float FCost => GCost + HCost;
}

