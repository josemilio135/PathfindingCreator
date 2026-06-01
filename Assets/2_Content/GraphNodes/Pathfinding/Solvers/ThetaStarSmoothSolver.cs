using System.Collections.Generic;
using UnityEngine;

public class ThetaStarSmoothSolver : IPathfindingSolver
{
    public List<BaseNode> Path { get; private set; } = new();

    readonly AStarSolver _aStar = new();

    private LayerMask _obstacleMask;
    private float _agentRadius;
    private float _agentHeight;

    public ThetaStarSmoothSolver(LayerMask obstacleMask, float agentRadius, float agentHeight)
    {
        _obstacleMask = obstacleMask;
        _agentRadius = agentRadius;
        _agentHeight = agentHeight;
    }

    public void Reset(NodesContainer container)
    {
        container.Reset();
        Path.Clear();
        _aStar.Reset(container);
    }

    public void Solve(BaseNode start, BaseNode end, NodesContainer container)
    {
        container.Reset();

        _aStar.Solve(start, end, container);

        List<BaseNode> rawPath = _aStar.Path;

        if (rawPath == null || rawPath.Count < 3)
        {
            Path = rawPath ?? new List<BaseNode>();
            return;
        }

        // String-pulling 
        Path.Add(rawPath[0]);
        int anchor = 0;

        for (int i = 1; i < rawPath.Count - 1; i++)
        {
            bool hasLOS = Perception.HasLineOfSight_Capsule(
                rawPath[anchor].Position,
                rawPath[i + 1].Position,
                _agentRadius,
                _agentHeight,
                _obstacleMask);

            if (!hasLOS)
            {
                Path.Add(rawPath[i]);
                anchor = i;
            }
        }

        Path.Add(rawPath[rawPath.Count - 1]);
    }
    public void ReconstructPath(BaseNode start, BaseNode end)
    {
    }
}