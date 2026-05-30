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
    public List<INode> LastPath { get; private set; }



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
    public System.Collections.IEnumerator FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        INode startNode =
            nodesContainer.FindClosestNode(startPosition);

        INode endNode =
            nodesContainer.FindClosestNode(targetPosition);

        if (startNode == null || endNode == null) yield break;

        IPathfindingSolver solver = CreateSolver();

        yield return solver.Solver(startNode, endNode, nodesContainer);

        LastPath = solver.Path;
    }
}


