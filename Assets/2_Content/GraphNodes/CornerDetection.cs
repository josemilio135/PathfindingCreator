// CornerDetection.cs

using System.Collections.Generic;
using UnityEngine;

public static class CornerDetection
{
    static readonly Collider[] _overlapResults = new Collider[256];
    static readonly Collider[] _capsuleHits = new Collider[64];
    static readonly RaycastHit[] _groundHits = new RaycastHit[64];

    const float VERTEX_MERGE = 0.025f;
    const float MIN_CORNER_ANGLE = 10f;
    const float EXTRA_OFFSET = 0.05f;

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;

        float mag = v.magnitude;

        if (mag <= 0.0001f)
            return Vector3.zero;

        return v / mag;
    }

    static Vector3 PerpendicularRight(Vector3 dir)
    {
        return new Vector3(dir.z, 0f, -dir.x);
    }

    static float Cross2D(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;

        return (ab.x * ac.z) - (ab.z * ac.x);
    }

    static bool IsWalkableNormal(Vector3 normal)
    {
        return Vector3.Dot(normal, Vector3.up) >= 0.55f;
    }

    static bool TryGetGround(
        Vector3 origin,
        float maxDistance,
        LayerMask walkableMask,
        out Vector3 point)
    {
        point = default;

        if (!Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            maxDistance,
            walkableMask,
            QueryTriggerInteraction.Ignore))
            return false;

        if (!IsWalkableNormal(hit.normal))
            return false;

        point = hit.point;

        return true;
    }

    static bool HasClearance(
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
            _capsuleHits,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = _capsuleHits[i];

            if (c == ignored)
                continue;

            Bounds b = c.bounds;

            float obstacleBottom = b.min.y;
            float obstacleTop = b.max.y;

            float agentBottom = point.y;
            float agentTop = point.y + agentHeight;

            if (obstacleBottom >= agentTop)
                continue;

            if (obstacleTop <= agentBottom)
                continue;

            return false;
        }

        return true;
    }

    static bool TrySnapToGround(
        Vector3 candidate,
        float agentHeight,
        float agentRadius,
        Collider ignored,
        LayerMask obstacleMask,
        LayerMask walkableMask,
        out Vector3 snapped)
    {
        snapped = candidate;

        Vector3 origin =
            candidate + Vector3.up * (agentHeight + 4f);

        int count = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            _groundHits,
            agentHeight + 100f,
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

            Vector3 point = hit.point;

            if (!HasClearance(
                point,
                agentHeight,
                agentRadius,
                ignored,
                obstacleMask))
                continue;

            if (point.y > bestY)
            {
                bestY = point.y;
                found = true;
            }
        }

        if (!found)
            return false;

        snapped.y = bestY;

        return true;
    }

    static bool NeedsToBeAvoided(
        Collider obstacle,
        float agentBottom,
        float agentHeight)
    {
        Bounds b = obstacle.bounds;

        float obstacleBottom = b.min.y;
        float obstacleTop = b.max.y;

        float agentTop =
            agentBottom + agentHeight;

        return obstacleTop > agentBottom &&
               obstacleBottom < agentTop;
    }

    static bool IsArchitecture(Collider collider)
    {
        return collider is MeshCollider mesh &&
               !mesh.convex;
    }

    static bool IsObstacle(Collider collider)
    {
        return collider is not MeshCollider mesh ||
               mesh.convex;
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

    static List<Vector3> BuildConvexHull(List<Vector3> points)
    {
        if (points.Count <= 3)
            return new(points);

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

        int lower = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lower &&
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

    static bool IntersectsAgentHeight(
        float obstacleBottom,
        float obstacleTop,
        float agentBottom,
        float agentTop)
    {
        return obstacleTop > agentBottom &&
               obstacleBottom < agentTop;
    }

    static void AddBoundsSliceCorners(
        Dictionary<Vector2Int, Vector3> unique,
        Bounds b,
        float sliceY)
    {
        AddUnique(unique,
            new Vector3(
                b.min.x,
                sliceY,
                b.min.z));

        AddUnique(unique,
            new Vector3(
                b.min.x,
                sliceY,
                b.max.z));

        AddUnique(unique,
            new Vector3(
                b.max.x,
                sliceY,
                b.min.z));

        AddUnique(unique,
            new Vector3(
                b.max.x,
                sliceY,
                b.max.z));
    }

    static void AddEdgeIntersectionPoint(
        Dictionary<Vector2Int, Vector3> unique,
        Vector3 a,
        Vector3 b,
        float agentBottom,
        float agentTop)
    {
        bool aInside =
            a.y >= agentBottom &&
            a.y <= agentTop;

        bool bInside =
            b.y >= agentBottom &&
            b.y <= agentTop;

        if (aInside)
        {
            a.y = agentBottom;
            AddUnique(unique, a);
        }

        if (bInside)
        {
            b.y = agentBottom;
            AddUnique(unique, b);
        }

        if ((a.y < agentBottom && b.y < agentBottom) ||
            (a.y > agentTop && b.y > agentTop))
            return;

        float[] slices =
        {
            agentBottom,
            agentTop
        };

        for (int i = 0; i < slices.Length; i++)
        {
            float slice = slices[i];

            float delta = b.y - a.y;

            if (Mathf.Abs(delta) <= 0.0001f)
                continue;

            float t =
                (slice - a.y) / delta;

            if (t < 0f || t > 1f)
                continue;

            Vector3 point =
                Vector3.Lerp(a, b, t);

            point.y = agentBottom;

            AddUnique(unique, point);
        }
    }

    static List<Vector3> ExtractContour(
        Collider collider,
        float agentBottom,
        float agentHeight,
        int curvedPrecision)
    {
        Dictionary<Vector2Int, Vector3> unique = new();

        float agentTop =
            agentBottom + agentHeight;

        switch (collider)
        {
            case BoxCollider box:
                {
                    Transform t = box.transform;

                    Vector3 c = box.center;
                    Vector3 h = box.size * 0.5f;

                    Vector3[] corners =
                    {
                    new(+h.x,+h.y,+h.z),
                    new(+h.x,+h.y,-h.z),
                    new(-h.x,+h.y,-h.z),
                    new(-h.x,+h.y,+h.z),

                    new(+h.x,-h.y,+h.z),
                    new(+h.x,-h.y,-h.z),
                    new(-h.x,-h.y,-h.z),
                    new(-h.x,-h.y,+h.z),
                };

                    bool foundValidVertex = false;

                    for (int i = 0; i < corners.Length; i++)
                    {
                        Vector3 world =
                            t.TransformPoint(c + corners[i]);

                        if (!IntersectsAgentHeight(
                            world.y,
                            world.y + 0.01f,
                            agentBottom,
                            agentTop))
                            continue;

                        world.y = agentBottom;

                        AddUnique(unique, world);

                        foundValidVertex = true;
                    }

                    if (!foundValidVertex)
                    {
                        Bounds b = box.bounds;

                        if (IntersectsAgentHeight(
                            b.min.y,
                            b.max.y,
                            agentBottom,
                            agentTop))
                        {
                            AddBoundsSliceCorners(
                                unique,
                                b,
                                agentBottom);
                        }
                    }

                    break;
                }

            case SphereCollider sphere:
                {
                    Transform t = sphere.transform;

                    Vector3 center =
                        t.TransformPoint(sphere.center);

                    float radius =
                        sphere.radius *
                        Mathf.Max(
                            t.lossyScale.x,
                            t.lossyScale.z);

                    float top =
                        center.y + radius;

                    float bottom =
                        center.y - radius;

                    if (!IntersectsAgentHeight(
                        bottom,
                        top,
                        agentBottom,
                        agentTop))
                        return new();

                    for (int i = 0; i < curvedPrecision; i++)
                    {
                        float a =
                            i / (float)curvedPrecision *
                            Mathf.PI * 2f;

                        Vector3 dir =
                            new(Mathf.Cos(a), 0f, Mathf.Sin(a));

                        Vector3 point =
                            center + dir * radius;

                        point.y = agentBottom;

                        AddUnique(unique, point);
                    }

                    break;
                }

            case CapsuleCollider capsule:
                {
                    Transform t = capsule.transform;

                    Vector3 center =
                        t.TransformPoint(capsule.center);

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

                    float capsuleBottom =
                        center.y - height * 0.5f;

                    float capsuleTop =
                        center.y + height * 0.5f;

                    if (!IntersectsAgentHeight(
                        capsuleBottom,
                        capsuleTop,
                        agentBottom,
                        agentTop))
                        return new();

                    Vector3 p1 =
                        center + axis * half;

                    Vector3 p2 =
                        center - axis * half;

                    for (int i = 0; i < curvedPrecision; i++)
                    {
                        float a =
                            i / (float)curvedPrecision *
                            Mathf.PI * 2f;

                        Vector3 dir =
                            (axis * Mathf.Cos(a)) +
                            (side * Mathf.Sin(a));

                        dir = Flatten(dir);

                        Vector3 aPoint =
                            p1 + dir * radius;

                        Vector3 bPoint =
                            p2 + dir * radius;

                        aPoint.y = agentBottom;
                        bPoint.y = agentBottom;

                        AddUnique(unique, aPoint);
                        AddUnique(unique, bPoint);
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

                    int[] tris =
                        mesh.triangles;

                    Transform t =
                        meshCollider.transform;

                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 a =
                            t.TransformPoint(
                                verts[tris[i]]);

                        Vector3 b =
                            t.TransformPoint(
                                verts[tris[i + 1]]);

                        Vector3 c =
                            t.TransformPoint(
                                verts[tris[i + 2]]);

                        AddEdgeIntersectionPoint(
                            unique,
                            a,
                            b,
                            agentBottom,
                            agentTop);

                        AddEdgeIntersectionPoint(
                            unique,
                            b,
                            c,
                            agentBottom,
                            agentTop);

                        AddEdgeIntersectionPoint(
                            unique,
                            c,
                            a,
                            agentBottom,
                            agentTop);
                    }

                    break;
                }

            default:
                {
                    Bounds b = collider.bounds;

                    if (!IntersectsAgentHeight(
                        b.min.y,
                        b.max.y,
                        agentBottom,
                        agentTop))
                        return new();

                    AddBoundsSliceCorners(
                        unique,
                        b,
                        agentBottom);

                    break;
                }
        }

        List<Vector3> result = new();

        foreach (var pair in unique)
            result.Add(pair.Value);

        return BuildConvexHull(result);
    }

    static IEnumerable<Vector3> GenerateArchitectureNodes(
        MeshCollider meshCollider,
        float agentBottom,
        float agentRadius,
        float agentHeight,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        if (meshCollider.sharedMesh == null)
            yield break;

        Mesh mesh =
            meshCollider.sharedMesh;

        Vector3[] verts =
            mesh.vertices;

        int[] tris =
            mesh.triangles;

        Transform t =
            meshCollider.transform;

        Dictionary<Vector2Int, Vector3> unique =
            new();

        Dictionary<Vector2Int, List<Vector3>> cornerMap =
            new();

        float agentTop =
            agentBottom + agentHeight;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a =
                t.TransformPoint(
                    verts[tris[i]]);

            Vector3 b =
                t.TransformPoint(
                    verts[tris[i + 1]]);

            Vector3 c =
                t.TransformPoint(
                    verts[tris[i + 2]]);

            Vector3 normal =
                Vector3.Cross(
                    b - a,
                    c - a).normalized;

            if (Mathf.Abs(normal.y) > 0.15f)
                continue;

            if ((a.y < agentBottom && b.y < agentBottom && c.y < agentBottom) ||
                (a.y > agentTop && b.y > agentTop && c.y > agentTop))
                continue;

            Vector3[] triVerts =
            {
                a, b, c
            };

            for (int v = 0; v < 3; v++)
            {
                Vector3 vertex =
                    triVerts[v];

                Vector2Int key = new(
                    Mathf.RoundToInt(vertex.x / VERTEX_MERGE),
                    Mathf.RoundToInt(vertex.z / VERTEX_MERGE));

                if (!cornerMap.TryGetValue(
                    key,
                    out List<Vector3> normals))
                {
                    normals = new();

                    cornerMap.Add(
                        key,
                        normals);
                }

                normals.Add(
                    Flatten(normal));
            }
        }

        foreach (var pair in cornerMap)
        {
            List<Vector3> normals =
                pair.Value;

            if (normals.Count < 2)
                continue;

            bool isCorner = false;

            Vector3 average =
                Vector3.zero;

            for (int i = 0; i < normals.Count; i++)
            {
                average += normals[i];

                for (int j = i + 1; j < normals.Count; j++)
                {
                    float angle =
                        Vector3.Angle(
                            normals[i],
                            normals[j]);

                    if (angle > 25f &&
                        angle < 175f)
                    {
                        isCorner = true;
                    }
                }
            }

            if (!isCorner)
                continue;

            average =
                Flatten(
                    average.normalized);

            Vector3 corner =
                new(
                    pair.Key.x * VERTEX_MERGE,
                    agentBottom,
                    pair.Key.y * VERTEX_MERGE);

            Vector3 candidate =
                corner + average *
                (agentRadius + EXTRA_OFFSET);

            if (!TrySnapToGround(
                candidate,
                agentHeight,
                agentRadius,
                meshCollider,
                obstacleMask,
                walkableMask,
                out candidate))
                continue;

            Vector2Int finalKey = new(
                Mathf.RoundToInt(candidate.x / VERTEX_MERGE),
                Mathf.RoundToInt(candidate.z / VERTEX_MERGE));

            if (unique.ContainsKey(finalKey))
                continue;

            unique.Add(finalKey, candidate);
        }

        foreach (var pair in unique)
            yield return pair.Value;
    }

    static IEnumerable<Vector3> GenerateCornerNodes(
        Collider collider,
        float agentBottom,
        float agentRadius,
        float agentHeight,
        int curvedPrecision,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        List<Vector3> contour =
            ExtractContour(
                collider,
                agentBottom,
                agentHeight,
                curvedPrecision);

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

            Vector3 outward =
                Flatten(
                    PerpendicularRight(dirA) +
                    PerpendicularRight(dirB));

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
        int curvedPrecision,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        if (!TryGetGround(
            origin + Vector3.up * 5f,
            100f,
            walkableMask,
            out Vector3 groundPoint))
            yield break;

        float agentBottom =
            groundPoint.y;

        Vector3 detectionOrigin =
            groundPoint + Vector3.up * (agentHeight * 0.5f);

        int count = Physics.OverlapSphereNonAlloc(
            detectionOrigin,
            viewRange,
            _overlapResults,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider collider =
                _overlapResults[i];

            if (!NeedsToBeAvoided(
                collider,
                agentBottom,
                agentHeight))
                continue;

            if (IsArchitecture(collider))
            {
                MeshCollider meshCollider =
                    (MeshCollider)collider;

                foreach (Vector3 node in GenerateArchitectureNodes(
                    meshCollider,
                    agentBottom,
                    agentRadius,
                    agentHeight,
                    obstacleMask,
                    walkableMask))
                {
                    Vector3 eye =
                        groundPoint +
                        Vector3.up * (agentHeight * 0.5f);

                    Vector3 target =
                        node +
                        Vector3.up * (agentHeight * 0.5f);

                    if (Perception.HasLineOfSight(
                        eye,
                        target,
                        obstacleMask))
                    {
                        yield return node;
                    }
                }

                continue;
            }

            if (!IsObstacle(collider))
                continue;

            foreach (Vector3 node in GenerateCornerNodes(
                collider,
                agentBottom,
                agentRadius,
                agentHeight,
                curvedPrecision,
                obstacleMask,
                walkableMask))
            {
                Vector3 eye =
                    groundPoint +
                    Vector3.up * (agentHeight * 0.5f);

                Vector3 target =
                    node +
                    Vector3.up * (agentHeight * 0.5f);

                if (Perception.HasLineOfSight(
                    eye,
                    target,
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