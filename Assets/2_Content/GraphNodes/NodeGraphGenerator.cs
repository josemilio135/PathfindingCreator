using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Tooltip("Layers used to detect the outline of these colliders and generate traversable nodes")]
    [SerializeField] LayerMask _obstacleMask;

    [Tooltip("Layers considered valid walkable ground surfaces used to snap nodes to the floor.")]
    [SerializeField] LayerMask _walkableMask;

    [Tooltip("Allows baking without requiring walkable floor layers.")]
    [SerializeField] bool _ignoreWalkableFloor = false;

    [Tooltip("Detection radius used to search for nearby colliders and evaluate their corners.")]
    [SerializeField, Min(0f)] float _viewRange = 15f;

    [Tooltip("Vertical space required for the agent to fit and move.")]
    [SerializeField, Min(0.1f)] float _agentHeight = 2f;

    [Tooltip("Horizontal collision size used for clearance checks and corner offset.")]
    [SerializeField, Min(0.05f)] float _agentRadius = 0.4f;

    [Tooltip("Prefab instantiated for every generated node.")]
    [SerializeField] GameObject _nodePrefab;

    [Tooltip("Amount of points generated around SphereColliders and CapsuleColliders.")]
    [SerializeField, Range(4, 32)] int _roundColliderPrecision = 8;

    [Tooltip("Distance required before nearby generated nodes merge together.")]
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;

    [Tooltip("Additional distance applied when pushing nodes away from corners.")]
    [SerializeField, Min(0.001f)] float _extraOffset = 0.05f;

    [Tooltip("Minimum angle required for a corner to be considered valid.")]
    [SerializeField, Range(0f, 180f)] float _minCornerAngle = 10f;

    [Tooltip("Automatically removes the previous baked node container before baking again.")]
    [SerializeField] bool _automaticUndo = false;

    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _lastGeneratedContainer == null;
    public float AgentRadius => _agentRadius;
    public float AgentHeight => _agentHeight;
    public bool IgnoreWalkableFloor => _ignoreWalkableFloor;
    public bool HasObstacleMask => _obstacleMask.value != 0;
    public bool HasWalkableMask => _walkableMask.value != 0;
    public bool CanBake =>
        HasObstacleMask && (_ignoreWalkableFloor || HasWalkableMask);

    Transform _lastGeneratedContainer;

    public void BakeOnlyThisNodes()
    {
        if (!ValidatePrefab()) return;
        if (_automaticUndo) UndoLastBake();

        List<Vector3> points = GetMergedCorners();

        InstantiateNodes(points);

        Debug.Log($"{name}: Local node bake completed. Generated {points.Count} nodes.");
    }

    public void BakeAllNodes()
    {
        if (!ValidatePrefab()) return;
        if (_automaticUndo) UndoLastBake();

        List<Vector3> points =
            NodeGraphBake.GenerateGraph(
                transform.position,
                _viewRange,
                _agentRadius,
                _agentHeight,
                _roundColliderPrecision,
                _nodeMergeDistance,
                _obstacleMask,
                _walkableMask);

        InstantiateNodes(points);

        Debug.Log($"{name}: Full area node bake completed. Generated {points.Count} nodes.");
    }

    public void UndoLastBake()
    {
        if (_lastGeneratedContainer == null)
        {
            Debug.Log(
                $"{name}: No baked node container to remove.");

            return;
        }

        string containerName =
            _lastGeneratedContainer.name;

#if UNITY_EDITOR
        DestroyImmediate(_lastGeneratedContainer.gameObject);
#else
    Destroy(_lastGeneratedContainer.gameObject);
#endif

        _lastGeneratedContainer = null;

        Debug.Log(
            $"{name}: Removed baked node container '{containerName}'.");
    }

    public IEnumerable<Vector3> GetVisibleCorners()
    {
        return CornerDetection.GetVisibleCorners(
            transform.position,
            _viewRange,
            _agentRadius,
            _agentHeight,
            _roundColliderPrecision,
            _obstacleMask,
            _walkableMask);
    }

    public List<Vector3> GetMergedCorners()
    {
        return CornerDetection.GetMergedCorners(
            GetVisibleCorners(),
            _nodeMergeDistance);
    }

    void InstantiateNodes(List<Vector3> points)
    {
        Transform container =
            CreateNodeContainer();

        for (int i = 0; i < points.Count; i++)
        {
#if UNITY_EDITOR
            GameObject node =
                (GameObject)UnityEditor.PrefabUtility
                .InstantiatePrefab(_nodePrefab, container);
#else
            GameObject node =
                Instantiate(_nodePrefab, container);
#endif

            node.transform.position =
                points[i];
        }
    }

    Transform CreateNodeContainer()
    {
        GameObject go =
            new($"{gameObject.name}_PathNodes");

        _lastGeneratedContainer =
            go.transform;

        return _lastGeneratedContainer;
    }

    bool ValidatePrefab()
    {
        if (_nodePrefab != null)
            return true;

        Debug.LogWarning(
            $"{name}: Cannot bake nodes because no Node Prefab is assigned.");

        return false;
    }
}