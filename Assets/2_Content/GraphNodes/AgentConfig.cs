using UnityEngine;

[CreateAssetMenu(fileName = "AgentConfig", menuName = "Pathfinding/Agent Config")]
public class AgentConfig : ScriptableObject
{
    [Tooltip("Layers used to detect the outline of these colliders and generate traversable nodes")]
    public LayerMask ObstacleMask;

    [Tooltip("Layers considered valid walkable ground surfaces used to snap nodes to the floor.")]
    public LayerMask WalkableMask;

    [Tooltip("Allows baking without requiring walkable floor layers.")]
    public bool IgnoreWalkableFloor = false;

    [Tooltip("Vertical space required for the agent to fit and move.")]
    [Min(0.1f)] public float Height = 2f;

    [Tooltip("Horizontal collision size used for clearance checks and corner offset.")]
    [Min(0.05f)] public float Radius = 0.4f;


}