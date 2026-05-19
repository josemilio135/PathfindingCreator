using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0f)] float _viewRange = 15f;

    [Header("Agent")]
    [SerializeField, Min(0.01f)] float _agentHeight = 2f;

    [SerializeField, Min(0.01f)] float _agentRadius = 0.4f;

    [Header("Nodes")]
    [SerializeField] GameObject _prefab;
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;
    [SerializeField] bool _automaticUndo = false;

    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _lastGeneratedContainer == null;
    public float AgentRadius => _agentRadius;
    public float AgentHeight => _agentHeight;

    Transform _lastGeneratedContainer;


    public void BakeOnlyThisNodes()
    {
        if (!ValidatePrefab()) return;
        if (_automaticUndo) UndoLastBake();

        var points = GetMergedCorners();
        InstantiateNodes(points);
        Debug.Log($"Bake local. {points.Count} nodos.");
    }

    public void BakeAllNodes()
    {
        if (!ValidatePrefab()) return;
        if (_automaticUndo) UndoLastBake();

        var points = NodeGraphBake.GenerateGraph(
            transform.position,
            _viewRange, _agentRadius, _agentHeight,
            _nodeMergeDistance, _obstacleMask);

        InstantiateNodes(points);
        Debug.Log($"Bake completo. {points.Count} nodos.");
    }

    public void UndoLastBake()
    {
        if (_lastGeneratedContainer == null) return;
#if UNITY_EDITOR
        DestroyImmediate(_lastGeneratedContainer.gameObject);
#else
        Destroy(_lastGeneratedContainer.gameObject);
#endif
        _lastGeneratedContainer = null;
        Debug.Log("Último bake borrado.");
    }

    public IEnumerable<Vector3> GetVisibleCorners() =>
        CornerDetection.GetVisibleCorners(
            transform.position, _viewRange,
            _agentRadius,  _agentHeight,
            _obstacleMask);

    public List<Vector3> GetMergedCorners() =>
        CornerDetection.GetMergedCorners(GetVisibleCorners(), _nodeMergeDistance);


    void InstantiateNodes(List<Vector3> points)
    {
        Transform container = CreateNodeContainer();

        for (int i = 0; i < points.Count; i++)
        {
#if UNITY_EDITOR
            var node = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_prefab, container);
#else
            var node = Instantiate(_prefab, container);
#endif
            node.transform.position = points[i];
        }
    }

    Transform CreateNodeContainer()
    {
        var container = new GameObject($"{gameObject.name}_PathNodes");
        _lastGeneratedContainer = container.transform;
        return _lastGeneratedContainer;
    }

    bool ValidatePrefab()
    {
        if (_prefab != null) return true;
        Debug.LogWarning("No prefab assigned.");
        return false;
    }
}