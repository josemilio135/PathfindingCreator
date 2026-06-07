using System.Collections.Generic;
using UnityEngine;

public class NodesContainer : MonoBehaviour
{
    [SerializeField] AgentConfig _agent;
    [SerializeField] List<BaseNode> _nodes = new();

    [Header("Show Gizmos")]
    [SerializeField] bool _drawAgentCapsules = true;
    [SerializeField] bool _drawConnections = true;

    public AgentConfig Agent
    {
        get => _agent;
        set => _agent = value;
    }

    public List<BaseNode> Nodes
    {
        get => _nodes;
        set => _nodes = value;
    }

    #region Editor Statistics
    public int ConnectionCount
    {
        get
        {
            int count = 0;
            foreach (BaseNode node in _nodes)
            {
                if (node == null) continue;
                count += node.Neighbors.Count;
            }
            return count;
        }
    }
    public float AverageConnections => _nodes.Count == 0 ? 0f : (float)ConnectionCount / _nodes.Count;
    public int EstimatedDFS_BFS => _nodes.Count + ConnectionCount;
    public int EstimatedDijkstra => Mathf.RoundToInt(ConnectionCount * Mathf.Log(Mathf.Max(_nodes.Count, 2), 2));
    public int EstimatedAStar => EstimatedDijkstra;
    public int EstimatedThetaStar => EstimatedDijkstra + ConnectionCount;
    public int EstimatedThetaStarSmooth => EstimatedDijkstra + (ConnectionCount * 2);
    #endregion

    public void Reset()
    {
        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;
            node.ResetPathFinding();
        }
    }

    [ContextMenu("Build Neighbors")]
    public void BuildNeighbors()
    {
        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;
            node.ClearNeighboirs();
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            BaseNode currentNode = _nodes[i];
            if (currentNode == null) continue;

            for (int j = 0; j < _nodes.Count; j++)
            {
                BaseNode otherNode = _nodes[j];
                if (otherNode == null || otherNode == currentNode) continue;

                bool hasLOS = Perception.HasLineOfSight_Capsule(
                    currentNode.Position, otherNode.Position,
                    _agent.Radius, _agent.Height, _agent.ObstacleMask);

                if (hasLOS) currentNode.AddNeighbor(otherNode);
            }
        }
    }

    public BaseNode FindClosestNode(Vector3 position)
    {
        BaseNode closest = null;
        float bestDistance = float.MaxValue;

        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;

            float distance = Vector3.SqrMagnitude(node.Position - position);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = node;
        }

        return closest;
    }

    public BaseNode FindBestNodeForTarget(
        Vector3 targetPosition, LayerMask obstacleMask, float agentRadius, float agentHeight)
    {
        BaseNode best = null;
        float bestScore = float.MaxValue;

        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;

            bool hasLOS = Perception.HasLineOfSight_Capsule(
                node.Position, targetPosition,
                agentRadius, agentHeight, obstacleMask);

            if (!hasLOS) continue;

            float score = Vector3.SqrMagnitude(node.Position - targetPosition);
            if (score >= bestScore) continue;

            bestScore = score;
            best = node;
        }

        return best ?? FindClosestNode(targetPosition);
    }

    #region Evaluate Nodes

    [ContextMenu("Remove Redundant Nodes")]
    public void RemoveRedundantNodes()
    {
        List<BaseNode> redundant = FindRedundantNodes();

        foreach (BaseNode node in redundant)
        {
            _nodes.Remove(node);

#if UNITY_EDITOR
            DestroyImmediate(node.gameObject);
#else
            Destroy(node.gameObject);
#endif
        }

        _nodes.RemoveAll(n => n == null);

        if (redundant.Count == 0) return;

        BuildNeighbors();
        Debug.Log($"Removed {redundant.Count} redundant nodes and rebuilt neighbors.");
    }

    List<BaseNode> FindRedundantNodes()
    {
        Dictionary<BaseNode, HashSet<BaseNode>> neighborSets = BuildNeighborSetCache();
        HashSet<BaseNode> toRemove = new();

        for (int i = 0; i < _nodes.Count; i++)
        {
            BaseNode a = _nodes[i];
            if (a == null || toRemove.Contains(a)) continue;

            for (int j = i + 1; j < _nodes.Count; j++)
            {
                BaseNode b = _nodes[j];
                if (b == null || toRemove.Contains(b)) continue;

                if (!AreNeighborSetsEquivalent(a, b, neighborSets)) continue;

                BaseNode victim = a.Neighbors.Count <= b.Neighbors.Count ? a : b;
                toRemove.Add(victim);
            }
        }

        foreach (BaseNode node in _nodes)
        {
            if (node == null || toRemove.Contains(node)) continue;
            if (node.Neighbors.Count == 0) toRemove.Add(node);
        }

        return new List<BaseNode>(toRemove);
    }

    Dictionary<BaseNode, HashSet<BaseNode>> BuildNeighborSetCache()
    {
        Dictionary<BaseNode, HashSet<BaseNode>> cache = new();

        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;
            cache[node] = new HashSet<BaseNode>(node.Neighbors);
        }

        return cache;
    }

    bool AreNeighborSetsEquivalent(
        BaseNode a, BaseNode b,
        Dictionary<BaseNode, HashSet<BaseNode>> sets)
    {
        if (!sets.TryGetValue(a, out HashSet<BaseNode> neighborsA)) return false;
        if (!sets.TryGetValue(b, out HashSet<BaseNode> neighborsB)) return false;

        if (neighborsA.Count == 0 || neighborsB.Count == 0) return false;

        bool aHasB = neighborsA.Contains(b);
        bool bHasA = neighborsB.Contains(a);

        if (aHasB) neighborsA.Remove(b);
        if (bHasA) neighborsB.Remove(a);

        bool equal = neighborsA.SetEquals(neighborsB);

        if (aHasB) neighborsA.Add(b);
        if (bHasA) neighborsB.Add(a);

        return equal;
    }

    #endregion

    #region Node Visibility

    [SerializeField, HideInInspector] bool _nodesVisible = true;

    public bool NodesVisible => _nodesVisible;

    [ContextMenu("Toggle Nodes Visibility")]
    public void ToggleNodesVisibility() => SetNodesVisible(!_nodesVisible);

    public void SetNodesVisible(bool visible)
    {
        _nodesVisible = visible;

        foreach (BaseNode node in _nodes)
        {
            if (node is NavNode navNode) navNode.SetVisible(visible);
        }
    }
    void OnEnable() => SetNodesVisible(_nodesVisible);

    #endregion

#if UNITY_EDITOR

    void OnValidate()
    {
        if (_agent != null) return;

        _drawAgentCapsules = false;
        _drawConnections = false;
    }

    void OnDrawGizmosSelected()
    {
        if (_agent == null || _nodes == null) return;

        foreach (BaseNode node in _nodes)
        {
            if (node == null) continue;

            if (_drawAgentCapsules) DrawCapsule(node.Position);

            if (!_drawConnections) continue;

            Gizmos.color = Color.green;

            foreach (BaseNode neighbour in node.Neighbors)
            {
                if (neighbour == null) continue;

                Gizmos.DrawLine(
                    node.Position + Vector3.up * (_agent.Height * 0.5f),
                    neighbour.Position + Vector3.up * (_agent.Height * 0.5f));
            }
        }

        void DrawCapsule(Vector3 position)
        {
            Gizmos.color = Color.cyan;

            Vector3 bottom = position + Vector3.up * _agent.Radius;
            Vector3 top = position + Vector3.up * (_agent.Height - _agent.Radius);

            Gizmos.DrawWireSphere(bottom, _agent.Radius);
            Gizmos.DrawWireSphere(top, _agent.Radius);

            Gizmos.DrawLine(bottom + Vector3.forward * _agent.Radius, top + Vector3.forward * _agent.Radius);
            Gizmos.DrawLine(bottom - Vector3.forward * _agent.Radius, top - Vector3.forward * _agent.Radius);
            Gizmos.DrawLine(bottom + Vector3.right * _agent.Radius, top + Vector3.right * _agent.Radius);
            Gizmos.DrawLine(bottom - Vector3.right * _agent.Radius, top - Vector3.right * _agent.Radius);
        }
    }
#endif
}