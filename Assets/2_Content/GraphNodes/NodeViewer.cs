using System.Collections.Generic;
using UnityEngine;

public class NodeViewer : MonoBehaviour
{
    [Header("Vision detection")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0f)] float _viewRange = 15f;
    [SerializeField, Min(0.0001f)] float _cornerOffset = .25f;

    [Header("Nodes")]
    [SerializeField] GameObject _prefab;
    [SerializeField, Min(0.01f)] float _nodeMergeDistance = 1f;

    [Header("Debug")]
    readonly List<GameObject> _spawnedNodes = new();


    public float ViewRange => _viewRange;
    public float NodeMergeDistance => _nodeMergeDistance;
    public bool IsClean => _spawnedNodes.Count == 0;

    public void BakeNodes()
    {
        if (_prefab == null)
        {
            Debug.LogWarning("No hay prefab asignado.");
            return;
        }

        ClearNodes();

        List<Vector3> mergedPoints = GetMergedCorners();

        for (int i = 0; i < mergedPoints.Count; i++)
        {
#if UNITY_EDITOR
            GameObject node =
                (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_prefab);
#else
            GameObject node = Instantiate(_prefab);
#endif

            node.transform.position = mergedPoints[i];
            node.transform.SetParent(transform);

            _spawnedNodes.Add(node);
        }

        Debug.Log($"Bake terminado. {_spawnedNodes.Count} nodos creados.");
    }


    public void ClearNodes()
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

    public IEnumerable<Vector3> GetVisibleCorners()
    {
        return CornerDetection.GetVisibleCorners(
            transform.position, _viewRange, _cornerOffset, _obstacleMask);
    }
    public List<Vector3> GetMergedCorners()
    {
        return CornerDetection.GetMergedCorners(
            GetVisibleCorners(),
            _nodeMergeDistance);
    }
}
