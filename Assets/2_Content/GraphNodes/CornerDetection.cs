using System.Collections.Generic;
using UnityEngine;

public static class CornerDetection
{
    static readonly Collider[] _overlapResults = new Collider[256];
    static readonly Collider[] _clearanceHits = new Collider[32];
    static readonly RaycastHit[] _groundHits = new RaycastHit[64];

    const float VERTEX_MERGE = 0.025f;
    const float MIN_CORNER_ANGLE = 15f;
    const float EXTRA_OFFSET = 0.05f;

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;

        float mag = v.magnitude;

        if (mag <= 0.0001f)
            return Vector3.zero;

        return v / mag;
    }

    static float Cross2D(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;

        return (ab.x * ac.z) - (ab.z * ac.x);
    }

    static Vector3 PerpendicularRight(Vector3 dir)
    {
        return new Vector3(dir.z, 0f, -dir.x);
    }

    static bool IsWalkableNormal(Vector3 normal)
    {
        return Vector3.Dot(normal, Vector3.up) >= 0.55f;
    }

    static bool IsObstacleRelevant(
        Collider collider,
        float agentBottom,
        float agentHeight)
    {
        Bounds b = collider.bounds;

        float obstacleBottom = b.min.y;
        float obstacleTop = b.max.y;

        float agentTop = agentBottom + agentHeight;

        return obstacleTop > agentBottom &&
               obstacleBottom < agentTop;
    }

    static bool IsInsideObstacle(
        Vector3 point,
        float agentHeight,
        float agentRadius,
        Collider ignored,
        LayerMask obstacleMask)
    {
        Vector3 bottom =
            point + Vector3.up * agentRadius;

        Vector3 top =
            point + Vector3.up * (agentHeight - agentRadius);

        int count = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            agentRadius,
            _clearanceHits,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = _clearanceHits[i];

            if (c == ignored)
                continue;

            if (!IsObstacleRelevant(
                c,
                point.y,
                agentHeight))
                continue;

            return true;
        }

        return false;
    }

    static bool TrySnapToGround(
        Vector3 point,
        float agentHeight,
        float agentRadius,
        Collider ignored,
        LayerMask obstacleMask,
        LayerMask walkableMask,
        out Vector3 snapped)
    {
        snapped = point;

        Vector3 origin =
            point + Vector3.up * (agentHeight + 2f);

        int count = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            _groundHits,
            agentHeight + 50f,
            walkableMask,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
            return false;

        float bestY = float.MinValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _groundHits[i];

            if (!IsWalkableNormal(hit.normal))
                continue;

            if (IsInsideObstacle(
                hit.point,
                agentHeight,
                agentRadius,
                ignored,
                obstacleMask))
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        if (!found)
            return false;

        snapped.y = bestY;

        return true;
    }

    static List<Vector3> BuildConvexHull(List<Vector3> points)
    {
        if (points.Count <= 3)
            return new List<Vector3>(points);

        points.Sort((a, b) =>
        {
            int x = a.x.CompareTo(b.x);

            if (x != 0)
                return x;

            return a.z.CompareTo(b.z);
        });

        List<Vector3> hull = new();

        for (int i = 0; i < points.Count; i++)
        {
            while (hull.Count >= 2 &&
                   Cross2D(
                       hull[hull.Count - 2],
                       hull[hull.Count - 1],
                       points[i]) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        int lowerCount = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lowerCount &&
                   Cross2D(
                       hull[hull.Count - 2],
                       hull[hull.Count - 1],
                       points[i]) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        hull.RemoveAt(hull.Count - 1);

        return hull;
    }

    static void AddUnique(
        Dictionary<Vector2Int, Vector3> map,
        Vector3 point)
    {
        Vector2Int key = new(
            Mathf.RoundToInt(point.x / VERTEX_MERGE),
            Mathf.RoundToInt(point.z / VERTEX_MERGE));

        if (!map.ContainsKey(key))
            map.Add(key, point);
    }

    static List<Vector3> ExtractContour(Collider collider)
    {
        Dictionary<Vector2Int, Vector3> unique = new();

        switch (collider)
        {
            case BoxCollider box:
                {
                    Transform t = box.transform;

                    Vector3 c = box.center;
                    Vector3 h = box.size * 0.5f;

                    Vector3[] corners =
                    {
                    new(+h.x, +h.y, +h.z),
                    new(+h.x, +h.y, -h.z),
                    new(-h.x, +h.y, -h.z),
                    new(-h.x, +h.y, +h.z),

                    new(+h.x, -h.y, +h.z),
                    new(+h.x, -h.y, -h.z),
                    new(-h.x, -h.y, -h.z),
                    new(-h.x, -h.y, +h.z),
                };

                    for (int i = 0; i < corners.Length; i++)
                    {
                        Vector3 world =
                            t.TransformPoint(c + corners[i]);

                        AddUnique(unique, world);
                    }

                    break;
                }

            case SphereCollider sphere:
                {
                    Transform t = sphere.transform;

                    float radius =
                        sphere.radius *
                        Mathf.Max(
                            t.lossyScale.x,
                            t.lossyScale.z);

                    Vector3 center =
                        t.TransformPoint(sphere.center);

                    const int steps = 24;

                    for (int i = 0; i < steps; i++)
                    {
                        float a =
                            i / (float)steps *
                            Mathf.PI * 2f;

                        Vector3 dir =
                            new(Mathf.Cos(a), 0f, Mathf.Sin(a));

                        AddUnique(unique,
                            center + dir * radius);
                    }

                    break;
                }

            case CapsuleCollider capsule:
                {
                    Transform t = capsule.transform;

                    Vector3 axis =
                        capsule.direction switch
                        {
                            0 => t.right,
                            2 => t.forward,
                            _ => t.up
                        };

                    axis = Flatten(axis);

                    Vector3 side =
                        PerpendicularRight(axis);

                    Vector3 center =
                        t.TransformPoint(capsule.center);

                    float radius =
                        capsule.radius *
                        Mathf.Max(
                            t.lossyScale.x,
                            t.lossyScale.z);

                    float height =
                        Mathf.Max(
                            capsule.height *
                            Mathf.Abs(t.lossyScale.y),
                            radius * 2f);

                    float half =
                        (height * 0.5f) - radius;

                    Vector3 p1 =
                        center + axis * half;

                    Vector3 p2 =
                        center - axis * half;

                    const int steps = 16;

                    for (int i = 0; i < steps; i++)
                    {
                        float a =
                            i / (float)steps *
                            Mathf.PI * 2f;

                        Vector3 dir =
                            (axis * Mathf.Cos(a)) +
                            (side * Mathf.Sin(a));

                        dir = Flatten(dir);

                        AddUnique(unique,
                            p1 + dir * radius);

                        AddUnique(unique,
                            p2 + dir * radius);
                    }

                    break;
                }

            case MeshCollider meshCollider:
                {
                    if (meshCollider.sharedMesh == null)
                        return new();

                    Mesh mesh =
                        meshCollider.sharedMesh;

                    Vector3[] verts =
                        mesh.vertices;

                    Transform t =
                        meshCollider.transform;

                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 world =
                            t.TransformPoint(verts[i]);

                        AddUnique(unique, world);
                    }

                    break;
                }

            default:
                {
                    Bounds b = collider.bounds;

                    AddUnique(unique,
                        new Vector3(b.min.x, 0f, b.min.z));

                    AddUnique(unique,
                        new Vector3(b.min.x, 0f, b.max.z));

                    AddUnique(unique,
                        new Vector3(b.max.x, 0f, b.min.z));

                    AddUnique(unique,
                        new Vector3(b.max.x, 0f, b.max.z));

                    break;
                }
        }

        List<Vector3> result = new();

        foreach (var pair in unique)
            result.Add(pair.Value);

        return BuildConvexHull(result);
    }

    static IEnumerable<Vector3> GenerateCornerNodes(
        Collider collider,
        float agentRadius,
        float agentHeight,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        List<Vector3> contour =
            ExtractContour(collider);

        if (contour.Count < 2)
            yield break;

        float offset =
            agentRadius + EXTRA_OFFSET;

        for (int i = 0; i < contour.Count; i++)
        {
            Vector3 prev =
                contour[(i - 1 + contour.Count) % contour.Count];

            Vector3 current =
                contour[i];

            Vector3 next =
                contour[(i + 1) % contour.Count];

            Vector3 dirA =
                Flatten(current - prev);

            Vector3 dirB =
                Flatten(next - current);

            if (dirA == Vector3.zero ||
                dirB == Vector3.zero)
                continue;

            float angle =
                Vector3.Angle(dirA, dirB);

            if (Mathf.Abs(angle - 180f) <= MIN_CORNER_ANGLE)
                continue;

            Vector3 outwardA =
                PerpendicularRight(dirA);

            Vector3 outwardB =
                PerpendicularRight(dirB);

            Vector3 outward =
                Flatten(outwardA + outwardB);

            if (outward == Vector3.zero)
                continue;

            Vector3 candidate =
                current + outward * offset;

            if (!TrySnapToGround(
                candidate,
                agentHeight,
                agentRadius,
                collider,
                obstacleMask,
                walkableMask,
                out candidate))
                continue;

            yield return candidate;
        }
    }

    public static IEnumerable<Vector3> GetVisibleCorners(
        Vector3 origin,
        float viewRange,
        float agentRadius,
        float agentHeight,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        int count = Physics.OverlapSphereNonAlloc(
            origin,
            viewRange,
            _overlapResults,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider collider =
                _overlapResults[i];

            foreach (Vector3 node in GenerateCornerNodes(
                collider,
                agentRadius,
                agentHeight,
                obstacleMask,
                walkableMask))
            {
                if (Perception.HasLineOfSight(
                    origin,
                    node,
                    obstacleMask))
                {
                    yield return node;
                }
            }
        }
    }

    public static List<Vector3> GetMergedCorners(
        IEnumerable<Vector3> points,
        float mergeDistance)
    {
        List<Vector3> merged = new();

        float sqr =
            mergeDistance * mergeDistance;

        foreach (Vector3 point in points)
        {
            bool found = false;

            for (int i = 0; i < merged.Count; i++)
            {
                Vector3 a = merged[i];
                Vector3 b = point;

                a.y = 0f;
                b.y = 0f;

                if ((a - b).sqrMagnitude > sqr)
                    continue;

                merged[i] =
                    (merged[i] + point) * 0.5f;

                found = true;
                break;
            }

            if (!found)
                merged.Add(point);
        }

        return merged;
    }
}