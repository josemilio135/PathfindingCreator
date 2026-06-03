using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extracts a horizontal 2D contour from a collider based on the agent vertical range.
/// 
/// The collider is sliced between the agent bottom and top height,
/// then converted into flattened XZ points suitable for pathfinding,
/// obstacle analysis, or visibility calculations.
///
/// Each collider type uses a specialized extraction method in order
/// to preserve its shape as accurately and efficiently as possible.
/// </summary>
public static class ColliderContourExtractor
{
    // Grid cell size used to snap nearby points together and avoid float-precision duplicates.
    const float VERTEX_SNAP = 0.025f;

    /// <summary>
    /// - Extracts a flattened horizontal contour from a collider within the agent vertical range.
    /// - The collider is vertically sliced using the agent height interval,
    /// then converted into XZ contour points depending on the collider geometry.
    /// - The resulting points are merged, cleaned and reduced into a convex hull
    /// suitable for navigation and obstacle processing.
    /// </summary>
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

    #region Box Collider
    static void ExtractBox(
        BoxCollider box, float agentBottom, float agentTop,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = box.transform;
        Vector3 center = box.center;
        Vector3 h = box.size * 0.5f;

        //Create 8 vertices of the box according to its size and local position
        Vector3[] localVerts =
        {
            new(+h.x, +h.y, +h.z), new(+h.x, +h.y, -h.z),
            new(-h.x, +h.y, -h.z), new(-h.x, +h.y, +h.z),
            new(+h.x, -h.y, +h.z), new(+h.x, -h.y, -h.z),
            new(-h.x, -h.y, -h.z), new(-h.x, -h.y, +h.z),
        };

        //It is possible that there are no vertices in the agents space
        bool haveVertsInBand = false;

        //Project in world space
        foreach (Vector3 local in localVerts)
        {
            Vector3 worldVerts = t.TransformPoint(center + local);

            if (!Intersects(worldVerts.y, worldVerts.y + 0.01f, agentBottom, agentTop))
                continue;

            worldVerts.y = agentBottom;
            AddUnique(unique, worldVerts);
            haveVertsInBand = true;
        }

        //If it does not find vertices, it creates approximate vertices.
        if (haveVertsInBand) return;
        Bounds b = box.bounds;
        if (Intersects(b.min.y, b.max.y, agentBottom, agentTop))
            AddBoundsCorners(unique, b, agentBottom);

    }
    /// <summary>
    /// Adds the four XZ corners of a bounding box at height <paramref name="y"/>
    /// to the unique point map. Used as a fallback when precise corner
    /// extraction is not available for the collider type.
    /// </summary>
    static void AddBoundsCorners(
     Dictionary<Vector2Int, Vector3> unique, Bounds b, float y)
    {
        AddUnique(unique, new Vector3(b.min.x, y, b.min.z));
        AddUnique(unique, new Vector3(b.min.x, y, b.max.z));
        AddUnique(unique, new Vector3(b.max.x, y, b.min.z));
        AddUnique(unique, new Vector3(b.max.x, y, b.max.z));
    }
    #endregion

    #region Sphere Collider
    static void ExtractSphere(
        SphereCollider sphere, float agentBottom, float agentTop,
        int precision,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = sphere.transform;
        Vector3 center = t.TransformPoint(sphere.center);
        float radius = sphere.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        if (!Intersects(center.y - radius, center.y + radius, agentBottom, agentTop))
            return;

        //create corners by precision
        for (int i = 0; i < precision; i++)
        {
            float angle = i / (float)precision * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            point.y = agentBottom;
            AddUnique(unique, point);
        }
    }
    #endregion

    #region Capsule Collider
    static void ExtractCapsule(
        CapsuleCollider capsule, float agentBottom, float agentTop,
        int precision,
        Dictionary<Vector2Int, Vector3> unique)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center);

        Vector3 directionAxis = capsule.direction switch
        {
            0 => t.right,
            2 => t.forward,
            _ => t.up
        };

        directionAxis = FlatNormalized(directionAxis);
        Vector3 perpendicularAxis = new(directionAxis.z, 0f, -directionAxis.x);

        float radius = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float height = Mathf.Max(capsule.height * Mathf.Abs(t.lossyScale.y), radius * 2f);
        float half = height * 0.5f - radius;

        if (!Intersects(center.y - height * 0.5f, center.y + height * 0.5f, agentBottom, agentTop))
            return;

        Vector3 centerUp = center + directionAxis * half;
        Vector3 centerDown = center - directionAxis * half;

        //create corners by precision * 2
        for (int i = 0; i < precision; i++)
        {
            float angle = i / (float)precision * Mathf.PI * 2f;
            Vector3 dir = FlatNormalized(
                directionAxis * Mathf.Cos(angle) + perpendicularAxis * Mathf.Sin(angle));

            Vector3 upperPoint = centerUp + dir * radius; upperPoint.y = agentBottom;
            Vector3 bottomPoint = centerDown + dir * radius; bottomPoint.y = agentBottom;

            AddUnique(unique, upperPoint);
            AddUnique(unique, bottomPoint);
        }
    }
    /// <summary>
    /// Normalizes a vector onto the horizontal plane by zeroing Y and re-normalizing.
    /// Returns Vector3.zero if the result would have near-zero magnitude.
    /// </summary>
    static Vector3 FlatNormalized(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }
    #endregion

    #region Mesh Collider
    static void ExtractMesh(
        MeshCollider meshCollider, float agentBottom, float agentTop,
        Dictionary<Vector2Int, Vector3> unique)
    {
        if (meshCollider.sharedMesh == null) return;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Transform t = meshCollider.transform;

        for (int i = 0; i < tris.Length; i += 3)
        {
            //Take 3 verts of each triangule
            Vector3 a = t.TransformPoint(verts[tris[i]]);
            Vector3 b = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 c = t.TransformPoint(verts[tris[i + 2]]);
            
            AddEdgeSlice(unique, a, b, agentBottom, agentTop);
            AddEdgeSlice(unique, b, c, agentBottom, agentTop);
            AddEdgeSlice(unique, c, a, agentBottom, agentTop);
        }
    }

    /// <summary>
    /// Returns the intersection points between a mesh edge and the agents height band,
    /// projected onto the floor plane. Used to find where mesh geometry crosses the agents space.
    /// </summary>
    static void AddEdgeSlice(
        Dictionary<Vector2Int, Vector3> unique,
        Vector3 a, Vector3 b,
        float agentBottom, float agentTop)
    {
        //If any end of the edge is already inside the band
        //It projects it to the ground and you store it directly.
        if (a.y >= agentBottom && a.y <= agentTop)
        {
            a.y = agentBottom;
            AddUnique(unique, a);
        }
        if (b.y >= agentBottom && b.y <= agentTop)
        {
            b.y = agentBottom;
            AddUnique(unique, b);
        }
        //Ignores edges outside the band
        if ((a.y < agentBottom && b.y < agentBottom)
            || (a.y > agentTop && b.y > agentTop))
            return;

        float[] slices = { agentBottom, agentTop };

        foreach (float slice in slices)
        {
            float heightDistance = b.y - a.y;
            if (Mathf.Abs(heightDistance) <= 0.0001f) continue;

            float t = (slice - a.y) / heightDistance;
            if (t < 0f || t > 1f) continue;

            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = agentBottom;
            AddUnique(unique, p);
        }
    }
    #endregion


    /// <summary>
    /// Using Andrew's Monotone Chain algorithm:
    /// Builds a 2D convex hull from a set of points. 
    /// Operates on the XZ plane, ignoring Y. Returns the minimal set of points
    /// that form the outermost boundary around all input points.
    /// </summary>
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
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        int lower = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lower && Cross2D(hull[^2], hull[^1], points[i]) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(points[i]);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }
    /// <summary>
    /// Positive means C is to the left of AB
    /// Negative means right. 
    /// Used by the convex hull to detect non-convex turns.
    /// </summary>
    static float Cross2D(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
    }

    /// <summary>
    /// Snaps a point to a discrete grid cell and adds it to the map if no point
    /// already occupies that cell. Prevents float-precision duplicates.
    /// </summary>
    static void AddUnique(Dictionary<Vector2Int, Vector3> map, Vector3 point)
    {
        Vector2Int key = new(
            Mathf.RoundToInt(point.x / VERTEX_SNAP),
            Mathf.RoundToInt(point.z / VERTEX_SNAP));

        map.TryAdd(key, point);
    }

    /// <summary>
    /// Checks whether two vertical ranges overlap.
    /// Used to determine if a collider section intersects
    /// the agent height interval.
    /// </summary>
    static bool Intersects(float minY, float maxY, float agentBottom, float agentTop)
    {
        return maxY > agentBottom && minY < agentTop;
    }

    #region Exception Collider
    /// <summary>
    /// Fallback extraction for unknown collider types.
    /// Uses the axis-aligned bounding box of the collider and adds
    /// its four horizontal corners at agent floor level if it
    /// intersects the agent height band.
    /// </summary>
    static void ExtractBounds(
       Bounds b, float agentBottom, float agentTop,
       Dictionary<Vector2Int, Vector3> unique)
    {
        if (Intersects(b.min.y, b.max.y, agentBottom, agentTop))
            AddBoundsCorners(unique, b, agentBottom);
    }
    #endregion
}