using System.Collections.Generic;

public class BFSSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();
    public Dictionary<BaseNode, BaseNode> ParentMap { get; private set; } = new();


    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        ParentMap.Clear();
    }

    public void Solve(BaseNode start, BaseNode end, NodesContainer container)
    {
        Reset(container);

        var queue = new Queue<BaseNode>();
        var enqueued = new HashSet<BaseNode>();

        queue.Enqueue(start);
        enqueued.Add(start);
        ParentMap[start] = null;

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
                queue.Enqueue(neighbor);
                ParentMap[neighbor] = currentNode;
            }

        }

    }
    public void ReconstructPath(BaseNode start, BaseNode end)
    {
        BaseNode node = end;
        while (node != null)
        {
            Path.Add(node);
            ParentMap.TryGetValue(node, out node);
        }

        Path.Reverse();
    }
}
