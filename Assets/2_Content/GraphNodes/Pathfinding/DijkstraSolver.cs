using System.Collections;
using System.Collections.Generic;

public class DijkstraSolver : IPathfindingSolver
{
    public List<INode> Path { get; private set; } = new();
    public Dictionary<INode, INode> ParentMap { get; private set; } = new();

    PriorityQueue<INode> _openSet = new();
    HashSet<INode> _inQueue = new();
    HashSet<INode> _closed = new();

    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        _openSet.Clear();
        _inQueue.Clear();
        _closed.Clear();
    }

    public IEnumerator Solver(INode start, INode end, NodesContainer container)
    {
        Reset(container);
        start.GCost = 0f;

        _openSet.Enqueue(start, 0f);
        _inQueue.Add(start);

        while (_openSet.Count > 0f)
        {
            INode currentNode = _openSet.Dequeue();
            _inQueue.Remove(currentNode);
            _closed.Add(currentNode);

            if (currentNode == end)
            {
                ReconstructPath(start, end);
                yield break;
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

    
    public void ReconstructPath(INode start, INode end)
    {
        INode node = end;
        while (node != null)
        {
            Path.Add(node);
            node = node.Parent;
        }

        Path.Reverse();
    }
}
