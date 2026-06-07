using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates navigable waypoint candidates around collider geometry
/// by detecting outward-facing corners and valid traversal positions.
/// 
/// Supports both convex obstacle contours and concave mesh architecture.
/// </summary>
public static class WaypointSampler
{
    //Merge nearby vertices, avoid floating duplicates
    const float VERTEX_SNAP = 0.025f;

    /// <summary>
    /// Generates navigable waypoint candidates from the outer corners
    /// of a convex obstacle contour.
    /// Each candidate is offset away from the obstacle surface
    /// using the averaged outward corner direction.
    /// </summary>
    public static IEnumerable<Vector3> SampleObstacleCorners(
        Collider obstacle,
        float agentBottom, AgentConfig agent,
        int curvedPrecision,
        SamplerSettings settings)
    {
        List<Vector3> contour = ColliderContourExtractor.Extract(
            obstacle, agentBottom, agent.Height, curvedPrecision);

        if (contour.Count < 2) yield break;

        float offset = agent.Radius + settings.ExtraOffset;

        for (int i = 0; i < contour.Count; i++)
        {
            //Three points to create a corner
            Vector3 prevVert = contour[(i - 1 + contour.Count) % contour.Count];
            Vector3 currentVert = contour[i];
            Vector3 nextVert = contour[(i + 1) % contour.Count];

            Vector3 dirIn = FlatNormalized(currentVert - prevVert);
            Vector3 dirOut = FlatNormalized(nextVert - currentVert);

            if (dirIn == Vector3.zero || dirOut == Vector3.zero) continue;

            float angle = Vector3.Angle(dirIn, dirOut);

            //Skip nearly straight segments
            if (Mathf.Abs(angle - 180f) <= settings.StraightAngleTolerance) continue;

            //Skip corners below the minimum threshold
            if (angle < settings.MinCornerAngle) continue;

            //Calculate direction out of the corner
            Vector3 outward = FlatNormalized(Perp(dirIn) + Perp(dirOut));
            if (outward == Vector3.zero) continue;

            //Miter-length keeps the offset consistent regardless of corner sharpness
            float cornerAngleRad = angle * Mathf.Deg2Rad;
            float miterLength = cornerAngleRad > 0.01f
                ? offset / Mathf.Sin(cornerAngleRad * 0.5f)
                : offset;

            miterLength = Mathf.Min(miterLength, offset * 4f);

            Vector3 candidate = currentVert + outward * miterLength;

            //Snap to walkable ground
            if (!AgentPhysics.TrySnapToGround(
                candidate, agent.Height, agent.Radius,
                obstacle, agent.ObstacleMask, agent.WalkableMask,
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
        float agentBottom, AgentConfig agent,
        SamplerSettings settings)
    {
        if (architecture.sharedMesh == null) yield break;

        Mesh mesh = architecture.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Transform t = architecture.transform;

        float agentTop = agentBottom + agent.Height;

        Dictionary<Vector2Int, List<Vector3>> vertexNormals = new();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = t.TransformPoint(verts[tris[i]]);
            Vector3 b = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 c = t.TransformPoint(verts[tris[i + 2]]);

            Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;

            //Skip floor/ceiling faces.
            if (Mathf.Abs(faceNormal.y) > 0.15f) continue;

            //Skip triangles fully outside agent height band
            if ((a.y < agentBottom && b.y < agentBottom && c.y < agentBottom) ||
                (a.y > agentTop && b.y > agentTop && c.y > agentTop))
                continue;

            foreach (Vector3 vertex in new[] { a, b, c })
                AccumulateNormal(vertexNormals, vertex, FlatNormalized(faceNormal));
        }

        Dictionary<Vector2Int, Vector3> accepted = new();

        foreach (var (key, normals) in vertexNormals)
        {
            if (!IsCornerVertex(normals, settings.MinArchCornerAngle, out Vector3 averageNormal))
                continue;

            Vector3 corner = new(key.x * VERTEX_SNAP, agentBottom, key.y * VERTEX_SNAP);
            Vector3 candidate = corner + averageNormal * (agent.Radius + settings.ExtraOffset);

            if (!AgentPhysics.TrySnapToGround(
                candidate, agent.Height, agent.Radius,
                architecture, agent.ObstacleMask, agent.WalkableMask,
                out candidate))
                continue;

            Vector2Int finalKey = SnapKey(candidate);
            if (!accepted.ContainsKey(finalKey))
                accepted.Add(finalKey, candidate);
        }

        foreach (var pair in accepted) yield return pair.Value;
    }

    /// <summary>
    /// Determines whether a mesh vertex represents a real architectural corner
    /// by comparing the angular difference between connected wall normals.
    /// Also outputs the averaged outward-facing corner direction.
    /// </summary>
    static bool IsCornerVertex(
        List<Vector3> normals, float minAngle,
        out Vector3 averageNormal)
    {
        averageNormal = Vector3.zero;
        bool isCorner = false;

        for (int i = 0; i < normals.Count; i++)
        {
            averageNormal += normals[i];

            for (int j = i + 1; j < normals.Count; j++)
            {
                float angle = Vector3.Angle(normals[i], normals[j]);
                if (angle > minAngle && angle < 175f) isCorner = true;
            }
        }

        averageNormal = FlatNormalized(averageNormal.normalized);
        return isCorner;
    }

    /// <summary>
    /// Groups wall normals by snapped vertex position so nearby mesh vertices
    /// contribute to the same architectural corner analysis.
    /// </summary>
    static void AccumulateNormal(
        Dictionary<Vector2Int, List<Vector3>> map,
        Vector3 vertex, Vector3 normal)
    {
        Vector2Int key = SnapKey(vertex);

        if (!map.TryGetValue(key, out var list))
        {
            list = new();
            map.Add(key, list);
        }

        list.Add(normal);
    }

    /// <summary>
    /// Converts a world position into a discrete XZ grid coordinate
    /// used for spatial deduplication and stable hashing.
    /// </summary>
    static Vector2Int SnapKey(Vector3 v)
    {
        return new(Mathf.RoundToInt(v.x / VERTEX_SNAP),
                   Mathf.RoundToInt(v.z / VERTEX_SNAP));
    }

    /// <summary>
    /// Projects a vector onto the horizontal plane and normalizes it.
    /// Returns Vector3.zero if the horizontal magnitude is negligible.
    /// </summary>
    static Vector3 FlatNormalized(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        return mag > 0.0001f ? v / mag : Vector3.zero;
    }

    /// <summary>
    /// Returns the horizontal perpendicular direction of a vector on the XZ plane.
    /// </summary>
    static Vector3 Perp(Vector3 dir) => new(dir.z, 0f, -dir.x);
}