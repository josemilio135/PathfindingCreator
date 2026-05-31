using System.Collections;
using System.Collections.Generic;

public class BFSSolver : IPathfindingSolver
{
    public List<INode> Path { get; private set; } = new();
    public Dictionary<INode, INode> ParentMap { get; private set; } = new();


    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        ParentMap.Clear();
    }

    public void Solve(INode start, INode end, NodesContainer container)
    {
        Reset(container);

        var queue = new Queue<INode>();
        var enqueued = new HashSet<INode>();

        queue.Enqueue(start);
        enqueued.Add(start);
        ParentMap[start] = null;

        while (queue.Count > 0)
        {
            INode currentNode = queue.Dequeue();

            if (currentNode == end)
            {
                ReconstructPath(start, end);
                return;
            }

            var neighbors = currentNode.Neighbors;


            foreach (var neighbor in neighbors)
            {
                if (enqueued.Contains(neighbor)) continue;

                enqueued.Add(neighbor);
                queue.Enqueue(neighbor);
                ParentMap[neighbor] = currentNode;
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
