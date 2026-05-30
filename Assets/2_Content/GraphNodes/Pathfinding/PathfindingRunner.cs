using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingRunner : MonoBehaviour
{
    public enum SolverType
    {
        DFS,
        BFS,
        Dijkstra,
        AStar,
        ThetaStar
    }

    [SerializeField] SolverType solverType = SolverType.AStar;
    [SerializeField] NodesContainer nodesContainer;

    IPathfindingSolver solver;


    private void Awake()
    {
        CreateSolver();
    }
    void StartSolving()
    {
        INode start = nodesContainer.startNode;
        INode end = nodesContainer.endNode;
        if (start == null || end == null) return;

        CreateSolver();
    }

    void CreateSolver()
    {
        IPathfindingSolver newSolver = solverType switch
        {
            SolverType.DFS => new DFSSolver(),
            SolverType.BFS => new BFSSolver(),


            _ => new DFSSolver()
        };

        solver = newSolver;
    }

}
public class NodesContainer
{
    public INode startNode;
    public INode endNode;

    public void Reset()
    {

    }
}
public interface IPathfindingSolver
{
    public List<INode> Path { get; }
    public Dictionary<INode, INode> ParentMap { get; }

    public void Reset(NodesContainer container);
    public IEnumerator Solver(INode start, INode end, NodesContainer container);
    void ReconstructPath(INode start, INode end);
}

