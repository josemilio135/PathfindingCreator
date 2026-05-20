using Unity.VisualScripting;
using UnityEngine;

public class NavNode : MonoBehaviour
{

    public NavNode Parent { get; set; }

    float movementCost;
    IRedOnlyList<NavNode> neighbors;
    Vector3 worldPosition;

    public float MovementCost => movementCost;
    public IRedOnlyList<NavNode> Neighbors => neighbors;
    public Vector3 WorldPosition => worldPosition;

    float GCost;
    float HCost;

    public void ResetPathFinding()
    {
        GCost=float.MaxValue;
        HCost = 0f;
        Parent = null;
    }
   // public void ClearNeighboirs() => neighbors.Clear;

}

public interface IRedOnlyList<T>
{

}