using UnityEngine;

public class CellNode : BaseNode
{
    [SerializeField] bool _isWalkable = true;
    public override float MovementCost => _isWalkable ? 1f : float.MaxValue;
}