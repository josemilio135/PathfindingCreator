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

    // Small buffer kept from the hit surface to avoid floating-point jitter against walls.
    const float SkinWidth = .02f;

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

    /// <summary>
    /// Returns true if an agent capsule can physically stand at this position:
    /// no vertical obstruction, no obstacle overlap, and valid ground below.
    /// </summary>
    public static bool IsWalkable(
        Vector3 position, float agentRadius, float agentHeight,
        LayerMask obstacleMask, LayerMask walkableMask)
    {
        if (Physics.Raycast(position + Vector3.up * 0.01f, Vector3.up, agentHeight,
            obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 bottom = position + Vector3.up * agentRadius;
        Vector3 top = position + Vector3.up * (agentHeight - agentRadius);

        if (Physics.CheckCapsule(bottom, top, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return TryGetGroundBelow(position + Vector3.up * 5f, 10f, walkableMask, out _);
    }

    /// <summary>
    /// Samples rings outward from target until isValid returns true. Generic
    /// spatial search, independent from any specific validity criteria.
    /// </summary>
    public static Vector3 FindNearestValidPosition(
        Vector3 target, float maxRadius, float stepRadius,
        System.Func<Vector3, bool> isValid, int samplesPerRing = 16)
    {
        if (isValid(target)) return target;

        for (float radius = stepRadius; radius <= maxRadius; radius += stepRadius)
        {
            for (int i = 0; i < samplesPerRing; i++)
            {
                float angle = (360f / samplesPerRing) * i;
                Vector3 candidate = target + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                if (isValid(candidate)) return candidate;
            }
        }

        return target;
    }

    /// <summary>
    /// Shortens a desired movement delta so the agents capsule never crosses an obstacle.
    /// </summary>   
    public static Vector3 ClampMovement(
        Vector3 position, Vector3 desiredDelta,
        float agentRadius, float agentHeight, LayerMask obstacleMask)
    {
        float distance = desiredDelta.magnitude;
        if (distance <= 0f) return desiredDelta;

        Vector3 direction = desiredDelta / distance;
        float castRadius = Mathf.Max(agentRadius - SkinWidth, 0.01f);
        Vector3 destination = position + desiredDelta;

        bool blocked = !Perception.HasLineOfSight_Capsule(
            position, destination, castRadius, agentHeight, obstacleMask, out RaycastHit hit);

        if (!blocked) return desiredDelta;

        bool movingAway = hit.normal != Vector3.zero && Vector3.Dot(direction, hit.normal) >= 0f;
        Vector3 result = movingAway ? desiredDelta : Vector3.ProjectOnPlane(desiredDelta, hit.normal);

        Vector3 bottom = position + result + Vector3.up * castRadius;
        Vector3 top = position + result + Vector3.up * (agentHeight - castRadius);

        if (Physics.CheckCapsule(bottom, top, castRadius, obstacleMask, QueryTriggerInteraction.Ignore))
            return Vector3.zero;

        return result;
    }
}