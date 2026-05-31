using System.Collections;
using System.Collections.Generic;

public class DFSSolver : IPathfindingSolver
{
    public List<INode> Path { get; private set; } = new();
    public Dictionary<INode, INode> ParentMap { get; private set; } = new();

    private readonly HashSet<INode> _visitedNodes = new();

    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        ParentMap.Clear();
        _visitedNodes.Clear();
    }

    public void Solve(INode start, INode end, NodesContainer container)
    {
        Reset(container);

        var stackNodes = new Stack<INode>();
        stackNodes.Push(start);

        ParentMap[start] = null;

        while (stackNodes.Count > 0)
        {
            var currentNode = stackNodes.Pop();

            if (_visitedNodes.Contains(currentNode)) continue;
            _visitedNodes.Add(currentNode);

            if (currentNode == end)
            {
                ReconstructPath(start, end);
                return;
            }

            var neighbors = currentNode.Neighbors;

            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                INode neighbor = neighbors[i];
                if (!_visitedNodes.Contains(neighbor))
                {
                    stackNodes.Push(neighbor);

                    if (!ParentMap.ContainsKey(neighbor))
                    {
                        ParentMap[neighbor] = currentNode;
                    }
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
            ParentMap.TryGetValue(node, out node);
        }

        Path.Reverse();
    }
}
