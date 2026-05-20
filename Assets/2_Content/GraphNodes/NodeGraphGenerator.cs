// NodeGraphGenerator.cs

using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField] LayerMask _walkableMask;

    [SerializeField, Min(0f)]
    float _viewRange = 15f;

    [Header("Agent")]
    [SerializeField, Min(0.1f)]
    float _agentHeight = 2f;

    [SerializeField, Min(0.05f)]
    float _agentRadius = 0.4f;

    [Header("Curved Precision")]
    [SerializeField, Range(4, 32)]
    int _curvedSurfacePrecision = 8;

    [Header("Nodes")]
    [SerializeField] GameObject _prefab;

    [SerializeField, Min(0.01f)]
    float _nodeMergeDistance = 1f;

    [SerializeField]
    bool _automaticUndo = false;

    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _lastGeneratedContainer == null;
    public float AgentRadius => _agentRadius;
    public float AgentHeight => _agentHeight;

    Transform _lastGeneratedContainer;

    public void BakeOnlyThisNodes()
    {
        if (!ValidatePrefab())
            return;

        if (_automaticUndo)
            UndoLastBake();

        List<Vector3> points =
            GetMergedCorners();

        InstantiateNodes(points);

        Debug.Log($"Bake local. {points.Count} nodos.");
    }

    public void BakeAllNodes()
    {
        if (!ValidatePrefab())
            return;

        if (_automaticUndo)
            UndoLastBake();

        List<Vector3> points =
            NodeGraphBake.GenerateGraph(
                transform.position,
                _viewRange,
                _agentRadius,
                _agentHeight,
                _curvedSurfacePrecision,
                _nodeMergeDistance,
                _obstacleMask,
                _walkableMask);

        InstantiateNodes(points);

        Debug.Log($"Bake completo. {points.Count} nodos.");
    }

    public void UndoLastBake()
    {
        if (_lastGeneratedContainer == null)
            return;

#if UNITY_EDITOR
        DestroyImmediate(_lastGeneratedContainer.gameObject);
#else
        Destroy(_lastGeneratedContainer.gameObject);
#endif

        _lastGeneratedContainer = null;
    }

    public IEnumerable<Vector3> GetVisibleCorners()
    {
        return CornerDetection.GetVisibleCorners(
            transform.position,
            _viewRange,
            _agentRadius,
            _agentHeight,
            _curvedSurfacePrecision,
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
                .InstantiatePrefab(_prefab, container);
#else
            GameObject node =
                Instantiate(_prefab, container);
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
        if (_prefab != null)
            return true;

        Debug.LogWarning("No prefab assigned.");

        return false;
    }
}