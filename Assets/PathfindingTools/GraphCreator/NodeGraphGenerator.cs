using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor-driven node baking tool used to generate waypoint graphs for pathfinding.
/// 
/// The generator scans nearby colliders inside the detection range, analyzes their
/// visible corners and surface outlines, then creates traversable node positions
/// around them. Generated points are merged together to avoid duplicate or overly
/// dense nodes before instantiating them as waypoint prefabs inside a container.
/// 
/// Supports visual debugging directly in the Unity Scene View, including:
/// - Detection range visualization
/// - Visible corner preview
/// - Node merge radius preview
/// - Agent size and clearance preview
/// 
/// Convex MeshColliders are treated as regular obstacles. Examples: rocks, debris, etc.
/// Non-convex MeshColliders are treated as architectural surfaces for more detailed
/// corner extraction. Examples: interior-houses, pro-builder, hallways, etc
/// 
/// Designed to work entirely from the Unity Inspector without entering Play Mode.
/// </summary>
public class NodeGraphGenerator : MonoBehaviour
{
    [SerializeField] AgentConfig _agent;

    [Tooltip("Detection radius used to search for nearby colliders and evaluate their corners.")]
    [SerializeField, Min(0f)] float _viewRange = 15f;

    [Tooltip("Prefab instantiated for every generated node.")]
    [SerializeField] BaseNode _nodePrefab;

    [Tooltip("Amount of points generated around SphereColliders and CapsuleColliders.")]
    [SerializeField, Range(4, 32)] int _roundColliderPrecision = 8;

    [Tooltip("Distance required before nearby generated nodes merge together.")]
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;

    [Tooltip("Sampling parameters: corner offsets, angle thresholds and tolerances.")]
    [SerializeField] SamplerSettings _samplerSettings = SamplerSettings.Default;

    [Tooltip("Container where baked nodes are stored.")]
    [SerializeField] NodesContainer _targetContainer;

    #region Editor Info
    //Read-only values exposed for the custom editor and debug visualization.
    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _targetContainer == null;
    public float AgentRadius => _agent != null ? _agent.Radius : 0f;
    public float AgentHeight => _agent != null ? _agent.Height : 0f;
    public bool HasObstacleMask => _agent != null && _agent.ObstacleMask.value != 0;
    public bool HasWalkableMask => _agent != null && _agent.WalkableMask.value != 0;
    public bool IgnoreWalkableFloor => _agent != null && _agent.IgnoreWalkableFloor;

    public bool CanBake =>
        _agent != null
        && _nodePrefab != null
        && HasObstacleMask
        && (_agent.IgnoreWalkableFloor || HasWalkableMask);
    #endregion


    /// <summary>
    /// Bakes nodes only from colliders visible within the local detection range.
    /// Reuses the existing container if one already exists.
    /// </summary>
    public void BakeOnlyThisNodes()
    {
        if (!ValidatePrefab()) return;

        List<Vector3> nodes = GetMergedCorners();
        InstantiateNodes(nodes);

        Debug.Log($"{name}: Local bake completed. Generated {nodes.Count} nodes.");
    }

    /// <summary>
    /// Bakes a full node graph for the surrounding area using BFS expansion.
    /// Reuses the existing container if one already exists.
    /// </summary>
    public void BakeAllNodes()
    {
        if (!ValidatePrefab()) return;

        List<Vector3> nodes = NodeGraphBake.GenerateGraph(
            transform.position,
            _viewRange, _agent,
            _roundColliderPrecision, _nodeMergeDistance,
            _samplerSettings);

        InstantiateNodes(nodes);

        Debug.Log($"{name}: Full bake completed. Generated {nodes.Count} nodes.");
    }

    /// <summary>
    /// Creates a new empty container and sets it as the active one.
    /// The previous container remains in the scene untouched,
    /// preserving any external references to it.
    /// </summary>
    public void NewContainer()
    {
        _targetContainer = null;
        InstantiateNodes(new List<Vector3>());

        Debug.Log($"{name}: New container created.");
    }

    /// <summary>
    /// Clears all nodes from the current container without destroying it.
    /// External references to the container remain valid.
    /// </summary>
    public void ClearContainer()
    {
        if (_targetContainer == null)
        {
            Debug.Log($"{name}: No container to clear.");
            return;
        }
        ClearContainerNodes(_targetContainer);
        Debug.Log($"{name}: Container cleared.");
    }

    /// <summary>
    /// Returns all detected visible corner positions before merge processing.
    /// Used for editor visualization and debugging.
    /// </summary>
    public IEnumerable<Vector3> GetVisibleCorners()
    {
        if (_agent == null) return System.Array.Empty<Vector3>();

        return NodeSampler.GetVisibleNodes(
            transform.position,
            _viewRange,
            _agent,
            _roundColliderPrecision,
            _samplerSettings);
    }

    /// <summary>
    /// Returns merged corner positions after distance-based cleanup.
    /// Used as the final waypoint positions before node instantiation.
    /// </summary>
    public List<Vector3> GetMergedCorners()
    {
        if (_agent == null) return new List<Vector3>();

        return NodeSampler.MergeNearbyNodes(
            GetVisibleCorners(),
            _nodeMergeDistance);
    }

    /// <summary>
    /// Clears the given container and repopulates it with nodes at the given positions.
    /// Creates a new container if none exists yet.
    /// </summary>
    void InstantiateNodes(List<Vector3> points)
    {
        NodesContainer container = GetOrCreateContainer();
        ClearContainerNodes(container);

        for (int i = 0; i < points.Count; i++)
        {
#if UNITY_EDITOR
            BaseNode node =
                (BaseNode)UnityEditor.PrefabUtility
                .InstantiatePrefab(_nodePrefab, container.transform);

            container.Nodes.Add(node);
            node.transform.position = points[i];
#endif
        }

        container.BuildNeighbors();
       // container.RemoveRedundantNodes();
    }

    /// <summary>
    /// Returns the existing active container, or creates a new one if none exists.
    /// </summary>
    NodesContainer GetOrCreateContainer()
    {
        if (_targetContainer != null) return _targetContainer;

        GameObject go = new("NODES_CONTAINER");
        NodesContainer container = go.AddComponent<NodesContainer>();
        container.Agent = _agent;
        _targetContainer = container;

        return container;
    }

    /// <summary>
    /// Destroys all child node GameObjects and clears the node list
    /// without destroying the container itself.
    /// </summary>
    void ClearContainerNodes(NodesContainer container)
    {
        if (container == null) return;

        for (int i = container.transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(container.transform.GetChild(i).gameObject);
#else
            Destroy(container.transform.GetChild(i).gameObject);
#endif
        }
        container.Nodes.Clear();
    }

    bool ValidatePrefab()
    {
        if (_nodePrefab != null) return true;
        Debug.LogWarning($"{name}: Cannot bake nodes because no Node Prefab is assigned.");

        return false;
    }
}