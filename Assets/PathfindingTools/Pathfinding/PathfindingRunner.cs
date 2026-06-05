using System.Collections.Generic;
using UnityEngine;

public class PathfindingRunner : MonoBehaviour
{
    [SerializeField] SolverType solverType = SolverType.AStar;
    [SerializeField] NodesContainer nodesContainer;
    public NodesContainer Container
    {
        get => nodesContainer;
        set => nodesContainer = value;
    }
    public enum SolverType
    {
        DFS,
        BFS,
        Dijkstra,
        AStar,
        ThetaStar,
        ThetaStarSmooth
    }
    public SolverType CurrentSolverType
    {
        get => solverType;
        set => solverType = value;
    }
    AgentConfig _agent => Container.Agent;

    IPathfindingSolver _cachedSolver;
    SolverType _cachedType;

    IPathfindingSolver CreateSolver()
    {
        return CurrentSolverType switch
        {
            SolverType.DFS => new DFSSolver(),
            SolverType.BFS => new BFSSolver(),
            SolverType.Dijkstra => new DijkstraSolver(),
            SolverType.AStar => new AStarSolver(),
            SolverType.ThetaStar => new ThetaStarSolver(_agent.ObstacleMask, _agent.Radius, _agent.Height),
            SolverType.ThetaStarSmooth => new ThetaStarSmoothSolver(_agent.ObstacleMask, _agent.Radius, _agent.Height),

            _ => new AStarSolver()
        };
    }
    IPathfindingSolver GetSolver()
    {
        if (_cachedSolver != null && _cachedType == CurrentSolverType)
            return _cachedSolver;

        _cachedSolver = CreateSolver();
        _cachedType = CurrentSolverType;
        return _cachedSolver;
    }
    public List<T> FindPath<T>(Vector3 startPosition, Vector3 targetPosition) where T : BaseNode
    {
        BaseNode startNode = nodesContainer.FindClosestNode(startPosition);
        BaseNode endNode = nodesContainer.FindClosestNode(targetPosition);

        if (startNode == null || endNode == null) return null;

        IPathfindingSolver solver = GetSolver();

        solver.Solve(startNode, endNode, nodesContainer);

        if (solver.Path == null || solver.Path.Count == 0) return null;

        List<T> result = new(solver.Path.Count);

        foreach (BaseNode node in solver.Path)
        {
            if (node is T typed) result.Add(typed);
        }

        return result;
    }
}