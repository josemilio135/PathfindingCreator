using System.Collections.Generic;
using UnityEngine;

public class NodesContainer : MonoBehaviour
{
    [Header("LOS")]
    [SerializeField] float _agentRadius;
    [SerializeField] float _agentHeight;

    [SerializeField] LayerMask _obstacleMask;
    [SerializeField] List<NavNode> _nodes = new();

    public List<NavNode> Nodes
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
        foreach (var node in _nodes)
        {
            if (node == null) continue;
            node.ResetPathFinding();
        }
    }

    [ContextMenu("Build Neighbors")]
    public void BuildNeighbors()
    {
        foreach (var node in _nodes)
        {
            if (node == null) continue;
            node.ClearNeighboirs();
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            INode currentNode = _nodes[i];

            if (currentNode == null) continue;

            for (int j = 0; j < _nodes.Count; j++)
            {
                INode otherNode = _nodes[j];

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

    public INode FindClosestNode(Vector3 position)
    {
        INode closest = null;
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




#if UNITY_EDITOR

    [SerializeField] bool _drawAgentCapsules = true;

    void OnDrawGizmosSelected()
    {
        if (_nodes == null) return;

        foreach (var node in _nodes)
        {
            if (node == null) continue;
            if (_drawAgentCapsules) DrawCapsule(node.Position);
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