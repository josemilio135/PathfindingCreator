using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flood-fills a navigable area starting from a seed position,
/// expanding the graph by discovering nodes visible from each visited point.
/// </summary>
public static class NodeGraphBake
{
    public static List<Vector3> GenerateGraph(
        Vector3 seedPosition,
        float viewRange,
        float agentRadius,
        float agentHeight,
        int curvedPrecision,
        float mergeDistance,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        Queue<Vector3> frontier = new();
        List<Vector3> graph = new();

        frontier.Enqueue(seedPosition);

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();

            List<Vector3> visible = NodeSampler.MergeNearbyNodes(
                NodeSampler.GetVisibleNodes(
                    current, viewRange,
                    agentRadius, agentHeight,
                    curvedPrecision,
                    obstacleMask, walkableMask),
                mergeDistance);

            foreach (Vector3 node in visible)
            {
                if (AlreadyInGraph(graph, node, mergeDistance)) continue;
                graph.Add(node);
                frontier.Enqueue(node);
            }
        }

        return graph;
    }

    static bool AlreadyInGraph(List<Vector3> graph, Vector3 node, float mergeDistance)
    {
        float sqr = mergeDistance * mergeDistance;

        foreach (Vector3 existing in graph)
        {
            Vector3 a = existing; a.y = 0f;
            Vector3 b = node; b.y = 0f;

            if ((a - b).sqrMagnitude <= sqr) return true;
        }

        return false;
    }
}