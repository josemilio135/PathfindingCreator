using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Header("Vision detection")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0f)] float _viewRange = 15f;
    [SerializeField, Min(0.0001f)] float _cornerOffset = .25f;

    [Header("Nodes")]
    [SerializeField] Transform _nodesContainer;
    [SerializeField] GameObject _prefab;
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;

    [Header("Debug")]
    readonly List<GameObject> _spawnedNodes = new();


    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _spawnedNodes.Count == 0;

    public void BakeOnlyThisNodes()
    {
        if (_prefab == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        ClearAllNodes();

        List<Vector3> points =
            GetMergedCorners();

        InstantiateNodes(points);

        Debug.Log($"Bake local. {points.Count} nodos creados.");
    }

    public void BakeAllNodes()
    {
        if (_prefab == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        ClearAllNodes();

        List<Vector3> points =
            NodeGraphBake.GenerateGraph(
                transform.position,
                _viewRange,
                _cornerOffset,
                _nodeMergeDistance,
                _obstacleMask);

        InstantiateNodes(points);

        Debug.Log($"Bake completo. {points.Count} nodos creados.");
    }






    public void ClearAllNodes()
    {
        int count = _spawnedNodes.Count;

        for (int i = _spawnedNodes.Count - 1; i >= 0; i--)
        {
            if (_spawnedNodes[i] == null) continue;

#if UNITY_EDITOR
            DestroyImmediate(_spawnedNodes[i]);
#else
            Destroy(_spawnedNodes[i]);
#endif
        }

        _spawnedNodes.Clear();

        Debug.Log($"{count} nodos borrados.");
    }
    void InstantiateNodes(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
#if UNITY_EDITOR
            Transform parentNodes = _nodesContainer ? _nodesContainer : transform;
            GameObject node =
                (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_prefab, parentNodes);
#else
        GameObject node = Instantiate(_prefab);
#endif

            node.transform.position = points[i];

            node.transform.SetParent(transform);

            _spawnedNodes.Add(node);
        }
    }

    public IEnumerable<Vector3> GetVisibleCorners()
    {
        return CornerDetection.GetVisibleCorners(
            transform.position, _viewRange, _cornerOffset, _obstacleMask);
    }
    public List<Vector3> GetMergedCorners()
    {
        return CornerDetection.GetMergedCorners(
            GetVisibleCorners(), _nodeMergeDistance);
    }


}
