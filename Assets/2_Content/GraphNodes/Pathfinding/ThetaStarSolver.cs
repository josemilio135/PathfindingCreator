using System.Collections.Generic;
using UnityEngine;

public class ThetaStarSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();

    readonly PriorityQueue<BaseNode> _openSet = new();
    readonly HashSet<BaseNode> _inQueue = new();
    readonly HashSet<BaseNode> _closed = new();

    private LayerMask _obstacleMask;
    private float _agentRadius;
    private float _agentHeight;

    public ThetaStarSolver(LayerMask obstacleMask, float agentRadius, float agentHeight)
    {
        _obstacleMask = obstacleMask;
        _agentRadius = agentRadius;
        _agentHeight = agentHeight;
    }

    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        _openSet.Clear();
        _inQueue.Clear();
        _closed.Clear();
    }

    public void Solve(BaseNode start, BaseNode end, NodesContainer container)
    {
        Reset(container);

        start.GCost = 0f;
        start.HCost = Heuristic(start, end);

        _openSet.Enqueue(start, start.FCost);
        _inQueue.Add(start);
        start.Parent = null;

        while (_openSet.Count > 0f)
        {
            BaseNode currentNode = _openSet.Dequeue();

            _inQueue.Remove(currentNode);
            _closed.Add(currentNode);

            if (currentNode == end)
            {
                ReconstructPath(start, end);
                return;
            }

            foreach (BaseNode neighbor in currentNode.Neighbors)
            {
                if (_closed.Contains(neighbor)) continue;

                BaseNode bestParent = currentNode;
                float bestCost = currentNode.GCost + EuclideanCost(currentNode, neighbor);

                if (currentNode.Parent)
                {
                    bool hasLOS =
                        Perception.HasLineOfSight_Capsule
                        (currentNode.Parent.Position, neighbor.Position,
                        _agentRadius, _agentHeight, _obstacleMask);

                    if (hasLOS)
                    {
                        float grandParentG =
                            currentNode.Parent.GCost + EuclideanCost(currentNode.Parent, neighbor);

                        if (grandParentG < bestCost)
                        {
                            bestParent = currentNode.Parent;
                            bestCost = grandParentG;
                        }
                    }
                }

                if (bestCost >= neighbor.GCost) continue;

                neighbor.GCost = bestCost;
                neighbor.HCost = Heuristic(neighbor, end);

                neighbor.Parent = bestParent;

                if (_inQueue.Contains(neighbor))
                {
                    _openSet.UpdatePriority(neighbor, neighbor.FCost);
                }
                else
                {
                    _openSet.Enqueue(neighbor, neighbor.FCost);
                    _inQueue.Add(neighbor);
                }
            }
        }
    }

    public void ReconstructPath(BaseNode start, BaseNode end)
    {
        BaseNode node = end;

        while (node != null)
        {
            Path.Add(node);
            node = node.Parent;
        }

        Path.Reverse();
    }

    static float Heuristic(BaseNode a, BaseNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    static float EuclideanCost(BaseNode a, BaseNode b)
    {
        return Vector3.Distance(a.Position, b.Position) * b.MovementCost;
    }
}
