using System.Collections.Generic;

public interface IPathfindingSolver
{
    public List<BaseNode> Path { get; }
    public Dictionary<BaseNode, BaseNode> ParentMap { get; }

    public void Reset(NodesContainer container);
    public void Solve(BaseNode start, BaseNode end, NodesContainer container);
    public void ReconstructPath(BaseNode start, BaseNode end);
}

