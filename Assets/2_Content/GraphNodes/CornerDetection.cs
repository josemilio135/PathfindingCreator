using System.Collections.Generic;
using UnityEngine;

public static class CornerDetection
{
    static readonly Collider[] _results = new Collider[64];

    public static IEnumerable<Vector3> GetVisibleCorners(Vector3 origin,
                                                         float viewRange,
                                                         float cornerOffset,
                                                         LayerMask obstacleMask)
    {

        // Detect colliders and store them in the results array.
        int collidersCount = Physics.OverlapSphereNonAlloc(
            origin, viewRange, _results, obstacleMask);

        for (int i = 0; i < collidersCount; i++)
        {
            BoxCollider box = _results[i] as BoxCollider;

            if (box == null) continue;

            Vector3[] corners = GetCorners(box);

            for (int j = 0; j < corners.Length; j++)
            {
                Vector3 realCorner = corners[j];

                Vector3 direction =
                    (realCorner - box.bounds.center).normalized;

                Vector3 offsetCorner =
                    realCorner + direction * cornerOffset;

                bool canSee =
                    Perception.HasLineOfSight(
                        origin, offsetCorner, obstacleMask);

                if (!canSee) continue;

                yield return offsetCorner;
            }
        }
    }

    static Vector3[] GetCorners(BoxCollider box)
    {
        Transform t = box.transform;

        Vector3 center = box.center;
        Vector3 half = box.size * 0.5f;

        Vector3[] localCorners =
        {
            center + new Vector3( half.x, 0f,  half.z),
            center + new Vector3( half.x, 0f, -half.z),
            center + new Vector3(-half.x, 0f,  half.z),
            center + new Vector3(-half.x, 0f, -half.z),
        };

        Vector3[] worldCorners = new Vector3[4];

        for (int i = 0; i < localCorners.Length; i++)
        {
            worldCorners[i] = t.TransformPoint(localCorners[i]);
        }

        return worldCorners;
    }
}