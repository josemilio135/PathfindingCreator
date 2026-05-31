using System.Collections;
using System.Collections.Generic;

public class DijkstraSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();
    public Dictionary<BaseNode, BaseNode> ParentMap { get; private set; } = new();

    PriorityQueue<BaseNode> _openSet = new();
    HashSet<BaseNode> _inQueue = new();
    HashSet<BaseNode> _closed = new();

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

        _openSet.Enqueue(start, 0f);
        _inQueue.Add(start);

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

            foreach(var neighbor in currentNode.Neighbors)
            {
                if (_closed.Contains(neighbor)) continue;

                float bestCost = currentNode.GCost + neighbor.MovementCost;
                if (bestCost >= neighbor.GCost) continue;

                neighbor.GCost = bestCost;
                neighbor.Parent = currentNode;

                if (_inQueue.Contains(neighbor))
                {
                    _openSet.UpdatePriority(neighbor, bestCost);
                }
                else
                {
                    _openSet.Enqueue(neighbor, bestCost);
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
}
