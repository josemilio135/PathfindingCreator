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

    IPathfindingSolver CreateSolver()
    {
        return solverType switch
        {
            SolverType.DFS => new DFSSolver(),
            SolverType.BFS => new BFSSolver(),
            SolverType.Dijkstra => new DijkstraSolver(),
            //SolverType.AStar => new AStarSolver(),
            //SolverType.ThetaStar => new ThetaStarSolver(),

            _ => new DFSSolver()
        };
    }
    public List<INode> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        INode startNode =
            nodesContainer.FindClosestNode(startPosition);

        INode endNode =
            nodesContainer.FindClosestNode(targetPosition);

        if (startNode == null || endNode == null) return null;

        IPathfindingSolver solver = CreateSolver();

        solver.Solve(startNode, endNode, nodesContainer);

        return solver.Path;
    }
}



