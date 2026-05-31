using System.Collections.Generic;

public class BFSSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();


    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
    }

    public void Solve(BaseNode start, BaseNode end, NodesContainer container)
    {
        Reset(container);

        var queue = new Queue<BaseNode>();
        var enqueued = new HashSet<BaseNode>();

        queue.Enqueue(start);
        enqueued.Add(start);

        start.Parent = null;

        while (queue.Count > 0)
        {
            BaseNode currentNode = queue.Dequeue();

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
                neighbor.Parent = currentNode;
                queue.Enqueue(neighbor);
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
