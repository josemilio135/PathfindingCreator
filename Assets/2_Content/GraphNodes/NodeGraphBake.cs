using System.Collections.Generic;
using UnityEngine;

public static class NodeGraphBake
{
    public static List<Vector3> GenerateGraph(
        Vector3 seedPosition,
        float viewRange,
        float agentRadius,
        float agentHeight,
        float mergeDistance,
        LayerMask obstacleMask)
    {
        var pending = new Queue<Vector3>();
        var graph = new List<Vector3>();

        pending.Enqueue(seedPosition);

        while (pending.Count > 0)
        {
            Vector3 current = pending.Dequeue();

            var visible = CornerDetection.GetMergedCorners(
                CornerDetection.GetVisibleCorners(
                    current, viewRange,
                    agentRadius, agentHeight,
                    obstacleMask),
                mergeDistance);

            for (int i = 0; i < visible.Count; i++)
            {
                Vector3 corner = visible[i];
                if (ContainsPoint(graph, corner, mergeDistance)) continue;
                graph.Add(corner);
                pending.Enqueue(corner);
            }
        }

        return graph;
    }

    static bool ContainsPoint(List<Vector3> points, Vector3 target, float range)
    {
        for (int i = 0; i < points.Count; i++)
            if (Perception.IsInRange(points[i], target, range))
                return true;
        return false;
    }
}