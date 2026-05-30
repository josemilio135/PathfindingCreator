using System.Collections.Generic;
using UnityEngine;

public interface INode
{
    Vector3 Position { get; }
    IReadOnlyList<INode> Neighbors { get; }
    float MovementCost { get; }
}

// public interface ICostNode<T> : INode
// {
//   public  float GCost { get; set; }
//   public  float HCost { get; set; }
//   public  float FCost => GCost + HCost;
// }
// 