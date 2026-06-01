using System.Collections.Generic;
using UnityEngine;

public class AStarSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();

    private readonly PriorityQueue<BaseNode> _openSet = new();
    private readonly HashSet<BaseNode> _inQueue = new();
    private readonly HashSet<BaseNode> _closed = new();

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


        while (_openSet.Count > 0)
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

                float bestCost =
                    currentNode.GCost + EuclideanCost(currentNode, neighbor);

                if (bestCost >= neighbor.GCost) continue;

                neighbor.GCost = bestCost;
                neighbor.HCost = Heuristic(neighbor, end);

                neighbor.Parent = currentNode;

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
    public static float Heuristic(BaseNode start, BaseNode end)
    {
        return Vector3.Distance(start.Position, end.Position);
    }
    public static float EuclideanCost(BaseNode start, BaseNode end)
    {
        return Heuristic(start, end) * end.MovementCost;
                                   //+ end.MovementCost;
    }
}
