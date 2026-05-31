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

                Vector3 from = currentNode.Position + Vector3.up * (_agentHeight * 0.5f);
                Vector3 to = otherNode.Position + Vector3.up * (_agentHeight * 0.5f);


                bool hasLOS =
                   Perception.HasLineOfSight_Capsule(
                       from, to, _agentRadius, _agentHeight,
                       _obstacleMask, out var hit);

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
}