using System.Collections.Generic;

public interface IPathfindingSolver
{
    public List<INode> Path { get; }
    public Dictionary<INode, INode> ParentMap { get; }

    public void Reset(NodesContainer container);
    public void Solve(INode start, INode end, NodesContainer container);
    public void ReconstructPath(INode start, INode end);
}

