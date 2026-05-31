using System.Collections.Generic;

public class DFSSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();

    private readonly HashSet<BaseNode> _visitedNodes = new();

    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        _visitedNodes.Clear();
    }

    public void Solve(BaseNode start, BaseNode end, NodesContainer container)
    {
        Reset(container);

        var stackNodes = new Stack<BaseNode>();
        stackNodes.Push(start);

        start.Parent = null;

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
                BaseNode neighbor = neighbors[i];
                if (!_visitedNodes.Contains(neighbor))
                {
                    if (neighbor.Parent == null && neighbor != start)
                    {
                        neighbor.Parent = currentNode;
                    }

                    stackNodes.Push(neighbor);
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