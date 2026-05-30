using System.Collections;
using System.Collections.Generic;

public interface IPathfindingSolver
{
    public List<INode> Path { get; }
    public Dictionary<INode, INode> ParentMap { get; }

    public void Reset(NodesContainer container);
    public IEnumerator Solver(INode start, INode end, NodesContainer container);
    void ReconstructPath(INode start, INode end);
}

