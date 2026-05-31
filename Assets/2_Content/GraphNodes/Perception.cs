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

    #endregion

    #region LOD Sphere
    public static bool HasLineOfSight_Sphere(
        Vector3 from, Vector3 to, float radius, LayerMask obstacleMask)
    {
        return HasLineOfSight_Sphere(
            from, to, radius, obstacleMask, out _);
    }
    public static bool HasLineOfSight_Sphere(
        Vector3 from, Vector3 to,
        float radius, LayerMask obstacleMask,
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
        if (radius <= 0f) return HasLineOfSight(from, to, obstacleMask, out hit);

        direction /= distance;

        bool blocked = Physics.SphereCast(
            from, radius, direction, out hit, distance, obstacleMask);

        return !blocked;
    }
    #endregion

    #region LOD_Capsule

    public static bool HasLineOfSight_Capsule(
        Vector3 from, Vector3 to, float radius, float height, LayerMask obstacleMask)
    {
        return HasLineOfSight_Capsule(
            from, to, radius, height, obstacleMask, out _);
    }

    public static bool HasLineOfSight_Capsule(
    Vector3 from, Vector3 to,
    float radius, float height,
    LayerMask obstacleMask, out RaycastHit hit)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        if (distance <= 0f)
        {
            hit = default;
            return true;
        }

        direction /= distance;

        Vector3 bottom = from + Vector3.up * radius;
        Vector3 top = from + Vector3.up * (height - radius);

        bool blocked =
            Physics.CapsuleCast(
                bottom, top, radius, direction,
                out hit, distance, obstacleMask,
                QueryTriggerInteraction.Ignore);

        return !blocked;
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
