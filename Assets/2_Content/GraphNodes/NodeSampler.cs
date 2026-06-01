using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Detects and collects valid waypoint positions visible from a given origin point.
/// Coordinates obstacle discovery, candidate sampling, line-of-sight filtering,
/// and proximity merging.
/// </summary>
public static class NodeSampler
{
    static readonly Collider[] _overlapBuffer = new Collider[256];

    /// <summary>
    /// Returns all waypoint candidates visible from the origin,
    /// before merge processing.
    /// </summary>
    public static IEnumerable<Vector3> GetVisibleNodes(
        Vector3 origin,
        float viewRange,
        AgentConfig agent,
        int curvedPrecision)
    {
        if (!AgentPhysics.TryGetGroundBelow(
            origin + Vector3.up * 5f, 100f, agent.WalkableMask,
            out Vector3 groundPoint))
            yield break;

        float agentBottom = groundPoint.y;
        // Vector3 eyeOrigin = groundPoint + Vector3.up * (agentHeight * 0.5f);

        int count = Physics.OverlapSphereNonAlloc(
            //eyeOrigin,
            groundPoint,
            viewRange,
            _overlapBuffer, agent.ObstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider obstacle = _overlapBuffer[i];

            if (!AgentPhysics.ColliderBlocksAgent(obstacle, agentBottom, agent.Height))
                continue;

            IEnumerable<Vector3> candidates;

            if (obstacle is MeshCollider { convex: false } meshCollider)
            {
                candidates = WaypointSampler.SampleArchitectureCorners(
                    meshCollider, agentBottom, agent);
            }
            else
            {
                candidates = WaypointSampler.SampleObstacleCorners(
                    obstacle, agentBottom, agent, curvedPrecision);
            }

            foreach (Vector3 node in candidates)
            {
                // Vector3 eyeTarget = node + Vector3.up * (agentHeight * 0.5f);
                //
                // if (Perception.HasLineOfSight(eyeOrigin, eyeTarget, obstacleMask))
                //     yield return node;

                if (Perception.HasLineOfSight_Capsule(
                   groundPoint, node, agent.Radius, agent.Height, agent.ObstacleMask))
                {
                    yield return node;
                }
            }
        }
    }

    /// <summary>
    /// Merges nodes that are within <paramref name="mergeRadius"/> of each other
    /// into a single averaged position.
    /// </summary>
    public static List<Vector3> MergeNearbyNodes(
        IEnumerable<Vector3> nodes,
        float mergeRadius)
    {
        List<Vector3> merged = new();
        float sqrRadius = mergeRadius * mergeRadius;

        foreach (Vector3 node in nodes)
        {
            bool absorbed = false;

            for (int i = 0; i < merged.Count; i++)
            {
                Vector3 a = merged[i]; a.y = 0f;
                Vector3 b = node; b.y = 0f;

                if ((a - b).sqrMagnitude > sqrRadius) continue;

                merged[i] = (merged[i] + node) * 0.5f;
                absorbed = true;
                break;
            }

            if (!absorbed) merged.Add(node);
        }

        return merged;
    }
}