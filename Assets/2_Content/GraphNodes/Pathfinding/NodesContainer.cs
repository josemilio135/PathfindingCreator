using System.Collections.Generic;
using UnityEngine;

public class NodesContainer : MonoBehaviour
{
    [Header("LOS")]
    [SerializeField] float clearanceRadius = 0.25f;
    [SerializeField] LayerMask obstacleMask;

    [SerializeField] List<NavNode> nodes = new();
    public IReadOnlyList<INode> Nodes => nodes;


    public void Reset()
    {
        foreach (var node in nodes)
        {
            if (node == null) continue;
            node.ResetPathFinding();
        }
    }

    [ContextMenu("Build Neighbors")]
    public void BuildNeighbors()
    {
        foreach (var node in nodes)
        {
            if (node == null) continue;
            node.ClearNeighboirs();
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            INode currentNode = nodes[i];

            if (currentNode == null) continue;

            for (int j = 0; j < nodes.Count; j++)
            {
                INode otherNode = nodes[j];

                if (otherNode == null) continue;
                if (otherNode == currentNode) continue;

                bool hasLOS =
                    Perception.HasLineOfSight_Sphere(
                        currentNode.Position, otherNode.Position,
                        clearanceRadius, obstacleMask,
                        out RaycastHit hit);

                if (hasLOS) currentNode.AddNeighbor(otherNode);
            }
        }
        Debug.Log("Neighbors generated.");
    }

    public INode FindClosestNode(Vector3 position)
    {
        INode closest = null;
        float bestDistance = float.MaxValue;

        foreach (var node in nodes)
        {
            if (node == null) continue;

            float distance =  Vector3.SqrMagnitude(node.Position - position);

            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = node;
        }

        return closest;
    }
}