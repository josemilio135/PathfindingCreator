using System.Collections.Generic;
using UnityEngine;

public class NodesContainer : MonoBehaviour
{
    [Header("LOS")]
    [SerializeField] float _agentRadius;
    [SerializeField] float _agentHeight;

    [SerializeField] LayerMask _obstacleMask;
    [SerializeField] List<BaseNode> _nodes = new();

    public List<BaseNode> Nodes
    {
        get => _nodes;
        set => _nodes = value;
    }
    public float AgentRadius
    {
        get => _agentRadius;
        set => _agentRadius = value;
    }
    public float AgentHeight
    {
        get => _agentHeight;
        set => _agentHeight = value;
    }
    public LayerMask ObstacleMask
    {
        get => _obstacleMask;
        set => _obstacleMask = value;
    }

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
                        _agentRadius, _agentHeight, _obstacleMask);

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

    [SerializeField] bool _drawAgentCapsules = true;
    [SerializeField] bool _drawConnections = true;

    void OnDrawGizmosSelected()
    {
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
                    node.Position + Vector3.up * (_agentHeight * 0.5f),
                    neighbour.Position + Vector3.up * (_agentHeight * 0.5f));
            }
        }
        void DrawCapsule(Vector3 position)
        {
            Gizmos.color = Color.cyan;

            Vector3 bottom = position + Vector3.up * _agentRadius;
            Vector3 top = position + Vector3.up * (_agentHeight - _agentRadius);

            Gizmos.DrawWireSphere(bottom, _agentRadius);
            Gizmos.DrawWireSphere(top, _agentRadius);

            Gizmos.DrawLine(
                bottom + Vector3.forward * _agentRadius,
                top + Vector3.forward * _agentRadius);

            Gizmos.DrawLine(
                bottom - Vector3.forward * _agentRadius,
                top - Vector3.forward * _agentRadius);

            Gizmos.DrawLine(
                bottom + Vector3.right * _agentRadius,
                top + Vector3.right * _agentRadius);

            Gizmos.DrawLine(
                bottom - Vector3.right * _agentRadius,
                top - Vector3.right * _agentRadius);
        }
    }
#endif
}