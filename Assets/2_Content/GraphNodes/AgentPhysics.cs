using UnityEngine;
/// <summary>
/// Physics queries related to agent movement: ground snapping,
/// clearance validation, and walkable surface detection.
/// </summary>
public static class AgentPhysics
{
    static readonly Collider[] _capsuleBuffer = new Collider[64];
    static readonly RaycastHit[] _groundBuffer = new RaycastHit[64];

    /// <summary>
    /// Attempts to find a walkable ground point directly below the origin.
    /// </summary>
    public static bool TryGetGroundBelow(
        Vector3 origin,  float maxDistance,
        LayerMask walkableMask, out Vector3 groundPoint)
    {
        groundPoint = default;

       // bool 
        if (!Physics.Raycast(
            origin, Vector3.down, out RaycastHit hit,
            maxDistance, walkableMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (!IsWalkableSurface(hit.normal))
            return false;

        groundPoint = hit.point;
        return true;
    }

    /// <summary>
    /// Snaps a candidate position to the highest valid walkable floor
    /// while verifying agent clearance above it.
    /// </summary>
    public static bool TrySnapToGround(
        Vector3 candidate,
        float agentHeight,
        float agentRadius,
        Collider ignoredCollider,
        LayerMask obstacleMask,
        LayerMask walkableMask,
        out Vector3 snapped)
    {
        snapped = candidate;

        Vector3 castOrigin = candidate + Vector3.up * (agentHeight + 4f);

        int hitCount = Physics.RaycastNonAlloc(
            castOrigin, Vector3.down, _groundBuffer,
            agentHeight + 100f, walkableMask, QueryTriggerInteraction.Ignore);

        if (hitCount <= 0) return false;

        float bestY = float.MinValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundBuffer[i];

            if (!IsWalkableSurface(hit.normal)) continue;

            if (!HasClearance(hit.point, agentHeight, agentRadius, ignoredCollider, obstacleMask))
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        if (!found) return false;

        snapped.y = bestY;
        return true;
    }

    /// <summary>
    /// Returns true if the obstacle's vertical range overlaps the agent's height band.
    /// </summary>
    public static bool ColliderBlocksAgent(
        Collider obstacle,
        float agentBottom,
        float agentHeight)
    {
        Bounds b = obstacle.bounds;
        return b.max.y > agentBottom &&
               b.min.y < agentBottom + agentHeight;
    }

    static bool HasClearance(
        Vector3 point,
        float agentHeight,
        float agentRadius,
        Collider ignored,
        LayerMask obstacleMask)
    {
        Vector3 bottom = point + Vector3.up * agentRadius;
        Vector3 top = point + Vector3.up * (agentHeight - agentRadius);

        int count = Physics.OverlapCapsuleNonAlloc(
            bottom, top, agentRadius,
            _capsuleBuffer, obstacleMask, QueryTriggerInteraction.Ignore);

        float agentTop = point.y + agentHeight;

        for (int i = 0; i < count; i++)
        {
            if (_capsuleBuffer[i] == ignored) continue;

            Bounds b = _capsuleBuffer[i].bounds;

            if (b.min.y >= agentTop || b.max.y <= point.y) continue;

            return false;
        }

        return true;
    }

    static bool IsWalkableSurface(Vector3 normal)
    {
        return Vector3.Dot(normal, Vector3.up) >= 0.55f;
    }
}
