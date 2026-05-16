using UnityEngine;

/// <summary>
/// Utility methods for AI perception and spatial detection.
/// </summary>
public static class Perception
{
    #region Line Of Sight

    /// <summary>
    /// Returns true if there are no obstacles between two points.
    /// </summary>
    public static bool HasLineOfSight(
        Vector3 from, Vector3 to,
        LayerMask obstacleMask)
    {
        return HasLineOfSight(
            from, to, obstacleMask, out _);
    }

    /// <summary>
    /// Returns true if there are no obstacles between two points.
    /// Also returns hit information if something blocks the view.
    /// </summary>
    public static bool HasLineOfSight(
        Vector3 from, Vector3 to,
        LayerMask obstacleMask,
        out RaycastHit hit)
    {
        Vector3 direction = to - from;

        float distance = direction.magnitude;

        // Same position.
        if (distance <= 0f)
        {
            hit = default;
            return true;
        }

        direction /= distance;

        bool blocked = Physics.Raycast(
            from, direction, out hit, distance, obstacleMask);

        return !blocked;
    }

    /// <summary>
    /// Returns true if a point on a collider is directly visible
    /// without being occluded by other geometry or by the collider itself.
    /// </summary>
    public static bool IsPointVisible(
        Vector3 from, Vector3 point,
        LayerMask obstacleMask, Collider expectedCollider)
    {
        Vector3 direction = point - from;

        float distance = direction.magnitude;

        if (distance <= 0f) return false;

        direction /= distance;

        bool hitSomething = Physics.Raycast(
            from, direction, out RaycastHit hit,
            distance + 0.01f, obstacleMask);

        if (!hitSomething) return false;

        if (hit.collider != expectedCollider) return false;

        float pointDistance =
            Vector3.Distance(from, point);

        float hitDistance =
            Vector3.Distance(from, hit.point);

        return Mathf.Abs(pointDistance - hitDistance) < 0.05f;
    }
    #endregion

    #region Range

    /// <summary>
    /// Returns true if the target is in range.
    /// </summary>
    public static bool IsInRange(Vector3 from, Vector3 to, float maxRange)
    {
        float sqrDistance = (to - from).sqrMagnitude;

        return sqrDistance <= maxRange * maxRange;
    }

    #endregion

    #region View Angle

    /// <summary>
    /// Returns true if the target is inside the observer field of view.
    /// </summary>
    public static bool IsInViewAngle(
        Vector3 observerPosition,
        Vector3 observerForward,
        Vector3 targetPosition,
        float fovAngle)
    {
        Vector3 directionToTarget = targetPosition - observerPosition;

        // Ignore height difference.
        directionToTarget.y = 0f;
        observerForward.y = 0f;

        directionToTarget.Normalize();
        observerForward.Normalize();

        float angle =
            Vector3.Angle(observerForward, directionToTarget);

        return angle <= fovAngle * 0.5f;
    }

    #endregion
}