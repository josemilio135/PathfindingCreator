using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Header("Vision detection")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0f)] float _viewRange = 15f;
    [SerializeField, Min(0.0001f)] float _cornerOffset = .25f;

    [Header("Nodes")]
    [SerializeField] GameObject _prefab;
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;
    [SerializeField] bool _automaticUndo = false;
    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _lastGeneratedContainer == null;

    //   readonly List<GameObject> _spawnedNodes = new();
    private Transform _lastGeneratedContainer;

    public void BakeOnlyThisNodes()
    {
        if (_prefab == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        if (_automaticUndo) UndoLastBake();

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

        if (_automaticUndo) UndoLastBake();

        List<Vector3> points =
            NodeGraphBake.GenerateGraph(
                transform.position,
                _viewRange, _cornerOffset, _nodeMergeDistance,
                _obstacleMask);

        InstantiateNodes(points);

        Debug.Log($"Bake completo. {points.Count} nodos creados.");
    }


    public void UndoLastBake()
    {
        if (_lastGeneratedContainer == null)
            return;

#if UNITY_EDITOR
        DestroyImmediate(
            _lastGeneratedContainer.gameObject);
#else
    Destroy(
        _lastGeneratedContainer.gameObject);
#endif

        Debug.Log("Último bake borrado.");

        _lastGeneratedContainer = null;
    }

    void InstantiateNodes(List<Vector3> points)
    {
        Transform container = CreateNodeContainer();
        for (int i = 0; i < points.Count; i++)
        {
#if UNITY_EDITOR
            GameObject node = (GameObject)UnityEditor
                .PrefabUtility.InstantiatePrefab(_prefab, container);
#else
        GameObject node = Instantiate(_prefab, container);
#endif

            node.transform.position = points[i];

            // _spawnedNodes.Add(node);
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
    Transform CreateNodeContainer()
    {
        GameObject container = new($"{gameObject.name}_PathNodes");
        _lastGeneratedContainer = container.transform;
        return _lastGeneratedContainer;
    }

}
