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

    #region Editor Stadistics
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

                if (otherNode == null) continue;
                if (otherNode == currentNode) continue;

                bool hasLOS =
                     Perception.HasLineOfSight_Capsule(
                        currentNode.Position, otherNode.Position,
                        _agent.Radius, _agent.Height, _agent.ObstacleMask);

                if (hasLOS) currentNode.AddNeighbor(otherNode);
            }
        }
        Debug.Log("Neighbors generated.");
    }

    public BaseNode FindClosestNode(Vector3 position)
    {
        BaseNode closest = null;
        float bestDistance = float.MaxValue;

        foreach (var node in _nodes)
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

        foreach (var node in _nodes)
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


#if UNITY_EDITOR

    void OnValidate()
    {
        if (_agent != null) return;

        _drawAgentCapsules = false;
        _drawConnections = false;
    }

    void OnDrawGizmosSelected()
    {
        if (_agent == null) return;
        if (_nodes == null) return;

        foreach (var node in _nodes)
        {
            if (node == null) continue;

            if (_drawAgentCapsules) DrawCapsule(node.Position);

            if (!_drawConnections) continue;

            Gizmos.color = Color.green;

            foreach (var neighbour in node.Neighbors)
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

            Gizmos.DrawLine(
                bottom + Vector3.forward * _agent.Radius,
                top + Vector3.forward * _agent.Radius);

            Gizmos.DrawLine(
                bottom - Vector3.forward * _agent.Radius,
                top - Vector3.forward * _agent.Radius);

            Gizmos.DrawLine(
                bottom + Vector3.right * _agent.Radius,
                top + Vector3.right * _agent.Radius);

            Gizmos.DrawLine(
                bottom - Vector3.right * _agent.Radius,
                top - Vector3.right * _agent.Radius);
        }
    }
#endif
}