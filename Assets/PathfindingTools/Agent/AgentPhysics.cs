using UnityEngine;
/// <summary>
/// Physics queries related to agent movement: ground snapping,
/// clearance validation, and walkable surface detection.
/// </summary>
public static class AgentPhysics
{
    // Pre-allocated buffer for obstacle colliders overlapping the agent capsule.
    static readonly Collider[] _agentColliderBuffer = new Collider[64];

    // Pre-allocated buffer for ground raycast hits below the agent.
    static readonly RaycastHit[] _groundBuffer = new RaycastHit[64];

    // Extra upward offset added to the raycast origin to ensure it starts
    // outside any geometry the candidate point may be clipping into.
    const float GROUND_CAST_MARGIN = 4f;

    //[Min: 0f] [Max: 1f] [Best value: 0.55f(33° angle)]
    const float WALKABLE_SLOPE_THRESHOLD = 0.55f;

    //Max distance to find a ground
    const float GROUND_CAST_DEPTH = 100f;


    /// <summary>
    /// Attempts to find a walkable ground point directly below the origin.
    /// </summary>
    public static bool TryGetGroundBelow(
        Vector3 origin, float maxDistance,
        LayerMask walkableMask, out Vector3 groundPoint)
    {
        groundPoint = default;

        bool foundGround =
            Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            maxDistance, walkableMask, QueryTriggerInteraction.Ignore);

        if (!foundGround) return false;

        if (!IsWalkableSurface(hit.normal)) return false;

        groundPoint = hit.point;
        return true;
    }

    /// <summary>
    /// Snaps a agent position to the highest valid walkable floor
    /// while verifying agent clearance above it.
    /// <paramref name="ignoredCollider"/> should be the obstacle that generated this candidate.
    /// </summary>
    public static bool TrySnapToGround(
        Vector3 agentPosition,
        float agentHeight,
        float agentRadius,
        Collider ignoredCollider,
        LayerMask obstacleMask,
        LayerMask walkableMask,
        out Vector3 snapped)
    {
        snapped = agentPosition;

        Vector3 upCastOrigin =
            agentPosition + Vector3.up * (agentHeight + GROUND_CAST_MARGIN);

        // Cast downward and collect all ground hits into _groundBuffer.
        int hitCount = Physics.RaycastNonAlloc(
            upCastOrigin, Vector3.down, _groundBuffer,
            agentHeight + GROUND_CAST_DEPTH,
            walkableMask, QueryTriggerInteraction.Ignore);

        if (hitCount <= 0) return false;

        float bestY = float.MinValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundBuffer[i];

            if (!IsWalkableSurface(hit.normal)) continue;

            if (!HasClearance(
                hit.point, agentHeight, agentRadius, ignoredCollider, obstacleMask))
                continue;

            //Keep the highest floor in case multiple floors are stacked vertically.
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        if (!found) return false;

        snapped.y = bestY; //Apply snap
        return true;
    }

    /// <summary>
    /// Returns true if the obstacles vertical range overlaps the agents height band.
    /// </summary>
    public static bool ColliderBlocksAgent(
        Collider obstacle, float agentBottom, float agentHeight)
    {
        Bounds b = obstacle.bounds;
        bool overlapsAgentHeight =
            b.max.y > agentBottom && b.min.y < agentBottom + agentHeight;

        return overlapsAgentHeight;
    }

    /// <summary>
    /// Verify that the agent can physically stand at a point
    /// both in terms of height and point of origin, avoiding obstacles.
    /// <paramref name="ignoredCollider"/> should be the obstacle that generated this candidate.
    /// </summary>
    static bool HasClearance(
        Vector3 agentPosition,
        float agentHeight,
        float agentRadius,
        Collider ignoredCollider,
        LayerMask obstacleMask)
    {
        //Define the two sphere centers of the agent capsule volume.
        Vector3 bottom = agentPosition + Vector3.up * agentRadius;
        Vector3 top = agentPosition + Vector3.up * (agentHeight - agentRadius);

        //Collect all obstacle colliders overlapping the agent capsule into _agentColliderBuffer.
        int obstacleCount = Physics.OverlapCapsuleNonAlloc(
            bottom, top, agentRadius,
            _agentColliderBuffer, obstacleMask, QueryTriggerInteraction.Ignore);

        float agentTop = agentPosition.y + agentHeight;

        for (int i = 0; i < obstacleCount; i++)
        {
            if (_agentColliderBuffer[i] == ignoredCollider) continue;

            Bounds b = _agentColliderBuffer[i].bounds;

            bool blocksAgentVertically =
                (b.min.y < agentTop && b.max.y > agentPosition.y);

            if (!blocksAgentVertically) continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if the surface normal is angled enough toward Vector3.up
    /// to be considered walkable ground. Rejects steep slopes and vertical walls.
    /// </summary>
    static bool IsWalkableSurface(Vector3 normal)
    {
        return Vector3.Dot(normal, Vector3.up) >= WALKABLE_SLOPE_THRESHOLD;
    }
}
