using System.Collections.Generic;
using UnityEngine;

public static class NodeGraphBake
{
    public static List<Vector3> GenerateGraph(
        Vector3 seedPosition,
        float viewRange, float cornerOffset, float mergeDistance,
        LayerMask obstacleMask)
    {
        Queue<Vector3> pendingPoints = new();
        List<Vector3> graphPoints = new();

        pendingPoints.Enqueue(seedPosition);

        while (pendingPoints.Count > 0)
        {
            Vector3 currentPoint = pendingPoints.Dequeue();

            List<Vector3> visibleCorners =
                CornerDetection.GetMergedCorners(
                    CornerDetection.GetVisibleCorners(
                        currentPoint, viewRange, cornerOffset, obstacleMask), mergeDistance);

            for (int i = 0; i < visibleCorners.Count; i++)
            {
                Vector3 corner = visibleCorners[i];

                bool alreadyExists =
                    ContainsPoint(graphPoints, corner, mergeDistance);

                if (alreadyExists) continue;

                graphPoints.Add(corner);

                pendingPoints.Enqueue(corner);
            }
        }

        return graphPoints;
    }

    static bool ContainsPoint(List<Vector3> points, Vector3 targetPoint, float range)
    {
        for (int i = 0; i < points.Count; i++)
        {
            bool isInRange =
                Perception.IsInRange(points[i], targetPoint, range);

            if (isInRange) return true;
        }

        return false;
    }
}