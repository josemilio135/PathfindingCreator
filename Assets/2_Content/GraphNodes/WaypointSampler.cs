using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Generates candidate waypoint positions around obstacle colliders,
/// both for regular convex obstacles and non-convex architecture surfaces.
/// </summary>
public static class WaypointSampler
{
    const float VERTEX_SNAP = 0.025f;
    const float CORNER_ANGLE = 25f;
    const float STRAIGHT_ANGLE = 10f;
    const float EXTRA_OFFSET = 0.05f;

    /// <summary>
    /// Samples waypoints around a convex obstacle's outline corners.
    /// </summary>
    public static IEnumerable<Vector3> SampleObstacleCorners(
        Collider obstacle,
        float agentBottom,
        float agentRadius,
        float agentHeight,
        int curvedPrecision,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        List<Vector3> contour = ColliderContourExtractor.Extract(
            obstacle, agentBottom, agentHeight, curvedPrecision);

        if (contour.Count < 2) yield break;

        float offset = agentRadius + EXTRA_OFFSET;

        for (int i = 0; i < contour.Count; i++)
        {
            Vector3 prev = contour[(i - 1 + contour.Count) % contour.Count];
            Vector3 current = contour[i];
            Vector3 next = contour[(i + 1) % contour.Count];

            Vector3 dirIn = FlatNormalized(current - prev);
            Vector3 dirOut = FlatNormalized(next - current);

            if (dirIn == Vector3.zero || dirOut == Vector3.zero) continue;

            float angle = Vector3.Angle(dirIn, dirOut);
            if (Mathf.Abs(angle - 180f) <= STRAIGHT_ANGLE) continue;

            Vector3 outward = FlatNormalized(Perp(dirIn) + Perp(dirOut));
            if (outward == Vector3.zero) continue;

            Vector3 candidate = current + outward * offset;

            if (!AgentPhysics.TrySnapToGround(
                candidate, agentHeight, agentRadius,
                obstacle, obstacleMask, walkableMask,
                out candidate))
                continue;

            yield return candidate;
        }
    }

    /// <summary>
    /// Samples waypoints from concave mesh architecture by detecting
    /// wall corners from vertex normals.
    /// </summary>
    public static IEnumerable<Vector3> SampleArchitectureCorners(
        MeshCollider architecture,
        float agentBottom,
        float agentRadius,
        float agentHeight,
        LayerMask obstacleMask,
        LayerMask walkableMask)
    {
        if (architecture.sharedMesh == null) yield break;

        Mesh mesh = architecture.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Transform t = architecture.transform;

        float agentTop = agentBottom + agentHeight;

        Dictionary<Vector2Int, List<Vector3>> vertexNormals = new();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = t.TransformPoint(verts[tris[i]]);
            Vector3 b = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 c = t.TransformPoint(verts[tris[i + 2]]);

            Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;

            // Skip floor/ceiling faces
            if (Mathf.Abs(faceNormal.y) > 0.15f) continue;

            // Skip triangles fully outside agent height band
            if ((a.y < agentBottom && b.y < agentBottom && c.y < agentBottom) ||
                (a.y > agentTop && b.y > agentTop && c.y > agentTop))
                continue;

            foreach (Vector3 vertex in new[] { a, b, c })
                AccumulateNormal(vertexNormals, vertex, FlatNormalized(faceNormal));
        }

        Dictionary<Vector2Int, Vector3> accepted = new();

        foreach (var (key, normals) in vertexNormals)
        {
            if (!IsCornerVertex(normals, out Vector3 averageNormal)) continue;

            Vector3 corner = new(key.x * VERTEX_SNAP, agentBottom, key.y * VERTEX_SNAP);
            Vector3 candidate = corner + averageNormal * (agentRadius + EXTRA_OFFSET);

            if (!AgentPhysics.TrySnapToGround(
                candidate, agentHeight, agentRadius,
                architecture, obstacleMask, walkableMask,
                out candidate))
                continue;

            Vector2Int finalKey = SnapKey(candidate);
            if (!accepted.ContainsKey(finalKey))
                accepted.Add(finalKey, candidate);
        }

        foreach (var pair in accepted)
            yield return pair.Value;
    }

    // -------------------------------------------------------------------------
    // Corner classification
    // -------------------------------------------------------------------------

    static bool IsCornerVertex(List<Vector3> normals, out Vector3 averageNormal)
    {
        averageNormal = Vector3.zero;
        bool isCorner = false;

        for (int i = 0; i < normals.Count; i++)
        {
            averageNormal += normals[i];

            for (int j = i + 1; j < normals.Count; j++)
            {
                float angle = Vector3.Angle(normals[i], normals[j]);
                if (angle > CORNER_ANGLE && angle < 175f)
                    isCorner = true;
            }
        }

        averageNormal = FlatNormalized(averageNormal.normalized);
        return isCorner;
    }

    static void AccumulateNormal(
        Dictionary<Vector2Int, List<Vector3>> map,
        Vector3 vertex,
        Vector3 normal)
    {
        Vector2Int key = SnapKey(vertex);

        if (!map.TryGetValue(key, out var list))
        {
            list = new();
            map.Add(key, list);
        }

        list.Add(normal);
    }

    // -------------------------------------------------------------------------
    // Math helpers
    // -------------------------------------------------------------------------

    static Vector2Int SnapKey(Vector3 v)
        => new(Mathf.RoundToInt(v.x / VERTEX_SNAP),
               Mathf.RoundToInt(v.z / VERTEX_SNAP));

    static Vector3 FlatNormalized(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }

    static Vector3 Perp(Vector3 dir) => new(dir.z, 0f, -dir.x);
}
