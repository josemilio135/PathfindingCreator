using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extracts the 2D horizontal contour of a collider at agent height level.
/// Each collider type has its own extraction strategy.
/// </summary>
public static class ColliderContourExtractor
{
    const float VERTEX_SNAP = 0.025f;

    public static List<Vector3> Extract(
        Collider collider,
        float agentBottom,
        float agentHeight,
        int curvedPrecision)
    {
        Dictionary<Vector2Int, Vector3> unique = new();
        float agentTop = agentBottom + agentHeight;

        switch (collider)
        {
            case BoxCollider box:
                ExtractBox(box, agentBottom, agentTop, unique);
                break;

            case SphereCollider sphere:
                ExtractSphere(sphere, agentBottom, agentTop, curvedPrecision, unique);
                break;

            case CapsuleCollider capsule:
                ExtractCapsule(capsule, agentBottom, agentTop, curvedPrecision, unique);
                break;

            case MeshCollider mesh:
                ExtractMesh(mesh, agentBottom, agentTop, unique);
                break;

            default:
                ExtractBounds(collider.bounds, agentBottom, agentTop, unique);
                break;
        }

        return BuildConvexHull(new List<Vector3>(unique.Values));
    }

   
    static void ExtractBox(
        BoxCollider box,
        float agentBottom,
        float agentTop,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = box.transform;
        Vector3 c = box.center;
        Vector3 h = box.size * 0.5f;

        Vector3[] localCorners =
        {
            new(+h.x, +h.y, +h.z), new(+h.x, +h.y, -h.z),
            new(-h.x, +h.y, -h.z), new(-h.x, +h.y, +h.z),
            new(+h.x, -h.y, +h.z), new(+h.x, -h.y, -h.z),
            new(-h.x, -h.y, -h.z), new(-h.x, -h.y, +h.z),
        };

        bool anyValid = false;

        foreach (Vector3 local in localCorners)
        {
            Vector3 world = t.TransformPoint(c + local);

            if (!Intersects(world.y, world.y + 0.01f, agentBottom, agentTop))
                continue;

            world.y = agentBottom;
            AddUnique(unique, world);
            anyValid = true;
        }

        if (!anyValid)
        {
            Bounds b = box.bounds;
            if (Intersects(b.min.y, b.max.y, agentBottom, agentTop))
                AddBoundsCorners(unique, b, agentBottom);
        }
    }

    static void ExtractSphere(
        SphereCollider sphere,
        float agentBottom,
        float agentTop,
        int precision,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = sphere.transform;
        Vector3 center = t.TransformPoint(sphere.center);
        float radius = sphere.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        if (!Intersects(center.y - radius, center.y + radius, agentBottom, agentTop))
            return;

        for (int i = 0; i < precision; i++)
        {
            float angle = i / (float)precision * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            point.y = agentBottom;
            AddUnique(unique, point);
        }
    }

    static void ExtractCapsule(
        CapsuleCollider capsule,
        float agentBottom,
        float agentTop,
        int precision,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center);

        Vector3 axis = capsule.direction switch
        {
            0 => t.right,
            2 => t.forward,
            _ => t.up
        };

        axis = FlatNormalized(axis);
        Vector3 side = new(axis.z, 0f, -axis.x);

        float radius = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float height = Mathf.Max(capsule.height * Mathf.Abs(t.lossyScale.y), radius * 2f);
        float half = height * 0.5f - radius;

        if (!Intersects(center.y - height * 0.5f, center.y + height * 0.5f, agentBottom, agentTop))
            return;

        Vector3 p1 = center + axis * half;
        Vector3 p2 = center - axis * half;

        for (int i = 0; i < precision; i++)
        {
            float angle = i / (float)precision * Mathf.PI * 2f;
            Vector3 dir = FlatNormalized(axis * Mathf.Cos(angle) + side * Mathf.Sin(angle));

            Vector3 a = p1 + dir * radius; a.y = agentBottom;
            Vector3 b = p2 + dir * radius; b.y = agentBottom;

            AddUnique(unique, a);
            AddUnique(unique, b);
        }
    }

    static void ExtractMesh(
        MeshCollider meshCollider,
        float agentBottom,
        float agentTop,
        Dictionary<Vector2Int, Vector3> unique)
    {
        if (meshCollider.sharedMesh == null) return;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Transform t = meshCollider.transform;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = t.TransformPoint(verts[tris[i]]);
            Vector3 b = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 c = t.TransformPoint(verts[tris[i + 2]]);

            AddEdgeSlice(unique, a, b, agentBottom, agentTop);
            AddEdgeSlice(unique, b, c, agentBottom, agentTop);
            AddEdgeSlice(unique, c, a, agentBottom, agentTop);
        }
    }

    static void ExtractBounds(
        Bounds b,
        float agentBottom,
        float agentTop,
        Dictionary<Vector2Int, Vector3> unique)
    {
        if (Intersects(b.min.y, b.max.y, agentBottom, agentTop))
            AddBoundsCorners(unique, b, agentBottom);
    }

  
    static void AddEdgeSlice(
        Dictionary<Vector2Int, Vector3> unique,
        Vector3 a, Vector3 b,
        float agentBottom, float agentTop)
    {
        if (a.y >= agentBottom && a.y <= agentTop) { a.y = agentBottom; AddUnique(unique, a); }
        if (b.y >= agentBottom && b.y <= agentTop) { b.y = agentBottom; AddUnique(unique, b); }

        if ((a.y < agentBottom && b.y < agentBottom) || (a.y > agentTop && b.y > agentTop))
            return;

        float[] slices = { agentBottom, agentTop };

        foreach (float slice in slices)
        {
            float delta = b.y - a.y;
            if (Mathf.Abs(delta) <= 0.0001f) continue;

            float t = (slice - a.y) / delta;
            if (t < 0f || t > 1f) continue;

            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = agentBottom;
            AddUnique(unique, p);
        }
    }

    static void AddBoundsCorners(
        Dictionary<Vector2Int, Vector3> unique,
        Bounds b,
        float y)
    {
        AddUnique(unique, new Vector3(b.min.x, y, b.min.z));
        AddUnique(unique, new Vector3(b.min.x, y, b.max.z));
        AddUnique(unique, new Vector3(b.max.x, y, b.min.z));
        AddUnique(unique, new Vector3(b.max.x, y, b.max.z));
    }

    static List<Vector3> BuildConvexHull(List<Vector3> points)
    {
        if (points.Count <= 3) return new(points);

        points.Sort((a, b) =>
        {
            int cmp = a.x.CompareTo(b.x);
            return cmp != 0 ? cmp : a.z.CompareTo(b.z);
        });

        List<Vector3> hull = new();

        for (int i = 0; i < points.Count; i++)
        {
            while (hull.Count >= 2 && Cross2D(hull[^2], hull[^1], points[i]) <= 0f)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(points[i]);
        }

        int lower = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lower && Cross2D(hull[^2], hull[^1], points[i]) <= 0f)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(points[i]);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    static void AddUnique(Dictionary<Vector2Int, Vector3> map, Vector3 point)
    {
        Vector2Int key = new(
            Mathf.RoundToInt(point.x / VERTEX_SNAP),
            Mathf.RoundToInt(point.z / VERTEX_SNAP));

        map.TryAdd(key, point);
    }

    static bool Intersects(float minY, float maxY, float agentBottom, float agentTop)
        => maxY > agentBottom && minY < agentTop;

    static float Cross2D(Vector3 a, Vector3 b, Vector3 c)
        => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);

    static Vector3 FlatNormalized(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }

    public static bool IsConvexObstacle(Collider c)
        => c is not MeshCollider m || m.convex;

    public static bool IsArchitectureSurface(Collider c)
        => c is MeshCollider m && !m.convex;
}