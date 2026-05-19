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
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        Queue<Vector3> pending = new();
        List<Vector3> graph = new();

        pending.Enqueue(seedPosition);

        while (pending.Count > 0)
        {
            Vector3 current =
                pending.Dequeue();

            List<Vector3> visible =
                CornerDetection.GetMergedCorners(
                    CornerDetection.GetVisibleCorners(
                        current,
                        viewRange,
                        agentRadius,
                        agentHeight,
                        obstacleMask,
                        walkableMask),
                    mergeDistance);

            for (int i = 0; i < visible.Count; i++)
            {
                Vector3 node = visible[i];

                if (ContainsPoint(
                    graph,
                    node,
                    mergeDistance))
                    continue;

                graph.Add(node);

                pending.Enqueue(node);
            }
        }

        return graph;
    }

    static bool ContainsPoint(
        List<Vector3> points,
        Vector3 target,
        float range)
    {
        float sqr = range * range;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 a = points[i];
            Vector3 b = target;

            a.y = 0f;
            b.y = 0f;

            if ((a - b).sqrMagnitude <= sqr)
                return true;
        }

        return false;
    }
}