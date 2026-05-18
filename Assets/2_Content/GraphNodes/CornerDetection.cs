using System.Collections.Generic;
using UnityEngine;

public static class CornerDetection
{
    static readonly Collider[] _results = new Collider[128];

    const int RADIAL_RAYS = 64;
    const float MAX_RADIUS = 256f;

    // Umbral: si la mesh tiene más vértices únicos en XZ que esto
    // se considera orgánica y usa raycast radial en lugar de análisis de vértices
    const int ORGANIC_VERTEX_THRESHOLD = 32;

    const float CORNER_ANGLE_THRESHOLD = 12f;

    static readonly RaycastHit[] _rayHits = new RaycastHit[32];

    // =========================================================
    // Helpers
    // =========================================================

    static Vector3 Flatten(Vector3 v, float y) { v.y = y; return v; }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v == Vector3.zero ? Vector3.zero : v.normalized;
    }

    // Apoya el nodo en el suelo con un raycast hacia abajo.
    // Si no hay suelo, devuelve el nodo con la Y original.
    static Vector3 SnapToGround(Vector3 node, LayerMask groundMask, float searchHeight = 2f)
    {
        Vector3 origin = node;
        origin.y += searchHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, searchHeight * 2f, groundMask))
            node.y = hit.point.y;

        return node;
    }

    // =========================================================
    // Colliders primitivos
    // =========================================================

    static IEnumerable<Vector3> GetBoxCorners(BoxCollider box, float offset, float y, LayerMask groundMask)
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

        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector3 worldCorner = t.TransformPoint(localCorners[i]);
            Vector3 dir = Horizontal(worldCorner - t.position);
            Vector3 node = worldCorner + dir * offset;
            node.y = y;
            yield return SnapToGround(node, groundMask);
        }
    }

    static IEnumerable<Vector3> GetSphereCorners(SphereCollider sphere, float offset, float y, LayerMask groundMask)
    {
        Transform t = sphere.transform;
        Vector3 center = t.TransformPoint(sphere.center);
        float radius = sphere.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8;
            Vector3 dir = Horizontal(t.TransformDirection(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))));
            Vector3 node = center + dir * (radius + offset);
            node.y = y;
            yield return SnapToGround(node, groundMask);
        }
    }

    static IEnumerable<Vector3> GetCapsuleCorners(CapsuleCollider capsule, float offset, float y, LayerMask groundMask)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center);
        float radius = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8;
            Vector3 dir = Horizontal(t.TransformDirection(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))));
            Vector3 node = center + dir * (radius + offset);
            node.y = y;
            yield return SnapToGround(node, groundMask);
        }
    }

    // =========================================================
    // MeshCollider: decide automáticamente entre geométrico u orgánico
    // =========================================================

    static IEnumerable<Vector3> GetMeshCorners(Collider collider, float offset, float y, LayerMask groundMask)
    {
        MeshCollider meshCollider = collider as MeshCollider;

        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            foreach (Vector3 p in GetOrganicMeshCorners(collider, offset, y, groundMask))
                yield return p;
            yield break;
        }

        int uniqueXZ = CountUniqueXZVertices(meshCollider.sharedMesh, collider.transform);

        IEnumerable<Vector3> source = uniqueXZ > ORGANIC_VERTEX_THRESHOLD
            ? GetOrganicMeshCorners(collider, offset, y, groundMask)
            : GetGeometricMeshCorners(meshCollider, offset, y, groundMask);

        foreach (Vector3 p in source)
            yield return p;
    }

    static int CountUniqueXZVertices(Mesh mesh, Transform t)
    {
        HashSet<Vector3Int> seen = new();
        foreach (Vector3 v in mesh.vertices)
            seen.Add(QuantizeXZ(t.TransformPoint(v)));
        return seen.Count;
    }

    // =========================================================
    // Geométrico: análisis de vértices (ProBuilder, paredes, objetos con esquinas)
    // =========================================================

    static IEnumerable<Vector3> GetGeometricMeshCorners(MeshCollider meshCollider, float offset, float y, LayerMask groundMask)
    {
        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Transform t = meshCollider.transform;

        // Agrupar vértices duplicados por posición mundo en XZ
        // (ProBuilder crea un vértice por cara; esto los fusiona)
        Dictionary<Vector3Int, Vector3> worldPosMap = new();
        Vector3Int[] vertexKeys = new Vector3Int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = t.TransformPoint(vertices[i]);
            Vector3Int key = QuantizeXZ(worldPos);
            vertexKeys[i] = key;
            if (!worldPosMap.ContainsKey(key))
                worldPosMap[key] = worldPos;
        }

        // Grafo de adyacencia entre vértices únicos en XZ
        Dictionary<Vector3Int, HashSet<Vector3Int>> adjacency = new();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(adjacency, vertexKeys[triangles[i]], vertexKeys[triangles[i + 1]]);
            AddEdge(adjacency, vertexKeys[triangles[i + 1]], vertexKeys[triangles[i + 2]]);
            AddEdge(adjacency, vertexKeys[triangles[i + 2]], vertexKeys[triangles[i]]);
        }

        // Detectar esquinas y calcular nodo con offset outward
        List<Vector3> candidates = new();
        Vector3 boundsCenter = meshCollider.bounds.center;

        foreach (var pair in adjacency)
        {
            if (!worldPosMap.TryGetValue(pair.Key, out Vector3 worldVertex)) continue;
            if (pair.Value.Count < 2) continue;

            List<Vector3> neighborDirs = new();

            foreach (Vector3Int nk in pair.Value)
            {
                if (!worldPosMap.TryGetValue(nk, out Vector3 nw)) continue;
                Vector3 dir = Horizontal(nw - worldVertex);
                if (dir != Vector3.zero) neighborDirs.Add(dir);
            }

            if (neighborDirs.Count < 2) continue;

            bool isCorner = false;

            for (int i = 0; i < neighborDirs.Count && !isCorner; i++)
                for (int j = i + 1; j < neighborDirs.Count && !isCorner; j++)
                    if (Mathf.Abs(Vector3.Angle(neighborDirs[i], neighborDirs[j]) - 180f) > CORNER_ANGLE_THRESHOLD)
                        isCorner = true;

            if (!isCorner) continue;

            Vector3 outward = ComputeOutward(worldVertex, neighborDirs, boundsCenter);
            if (outward == Vector3.zero) continue;

            Vector3 node = worldVertex + outward * offset;
            node.y = y;
            candidates.Add(SnapToGround(node, groundMask));
        }

        candidates = Simplify(candidates, offset * 1.5f);
        for (int i = 0; i < candidates.Count; i++)
            yield return candidates[i];
    }

    static Vector3 ComputeOutward(Vector3 vertexPos, List<Vector3> edgeDirs, Vector3 boundsCenter)
    {
        Vector3 accumulated = Vector3.zero;

        for (int i = 0; i < edgeDirs.Count; i++)
            for (int j = i + 1; j < edgeDirs.Count; j++)
            {
                Vector3 bisector = Horizontal(edgeDirs[i] + edgeDirs[j]);
                if (bisector == Vector3.zero)
                    bisector = new Vector3(-edgeDirs[i].z, 0f, edgeDirs[i].x);
                accumulated += bisector;
            }

        Vector3 candidate = Horizontal(accumulated);
        Vector3 toCenter = Horizontal(boundsCenter - vertexPos);

        if (toCenter != Vector3.zero && Vector3.Dot(candidate, toCenter) > 0f)
            candidate = -candidate;

        return candidate;
    }

    static void AddEdge(Dictionary<Vector3Int, HashSet<Vector3Int>> adjacency, Vector3Int a, Vector3Int b)
    {
        if (a == b) return;
        if (!adjacency.TryGetValue(a, out var sa)) adjacency[a] = sa = new();
        if (!adjacency.TryGetValue(b, out var sb)) adjacency[b] = sb = new();
        sa.Add(b);
        sb.Add(a);
    }

    // =========================================================
    // Orgánico: raycast radial desde el exterior hacia el collider
    // =========================================================

    static IEnumerable<Vector3> GetOrganicMeshCorners(Collider collider, float offset, float y, LayerMask groundMask)
    {
        Vector3 center = Flatten(collider.bounds.center, y);
        List<Vector3> points = new();

        for (int i = 0; i < RADIAL_RAYS; i++)
        {
            float angle = i * Mathf.PI * 2f / RADIAL_RAYS;
            Vector3 dir = Horizontal(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            Vector3 origin = center - dir * MAX_RADIUS;

            int hitCount = Physics.RaycastNonAlloc(origin, dir, _rayHits, MAX_RADIUS * 2f);

            float closest = float.MaxValue;
            RaycastHit selected = default;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit = _rayHits[j];
                if (hit.collider != collider || hit.distance >= closest) continue;
                closest = hit.distance;
                selected = hit;
            }

            if (closest == float.MaxValue) continue;

            Vector3 normal = Horizontal(selected.normal);
            if (normal == Vector3.zero) continue;

            Vector3 node = selected.point + normal * offset;
            node.y = y;
            points.Add(SnapToGround(node, groundMask));
        }

        points = Simplify(points, offset * 2f);
        for (int i = 0; i < points.Count; i++)
            yield return points[i];
    }

    // =========================================================
    // Utilidades
    // =========================================================

    static Vector3Int QuantizeXZ(Vector3 v) =>
        new(Mathf.RoundToInt(v.x * 1000f), 0, Mathf.RoundToInt(v.z * 1000f));

    static List<Vector3> Simplify(List<Vector3> points, float minDistance)
    {
        List<Vector3> result = new();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 cur = points[i];
            bool merged = false;

            for (int j = 0; j < result.Count; j++)
            {
                Vector3 a = result[j]; a.y = 0f;
                Vector3 b = cur; b.y = 0f;

                if ((a - b).sqrMagnitude <= minDistance * minDistance)
                {
                    merged = true;
                    break;
                }
            }

            if (!merged) result.Add(cur);
        }

        return result;
    }

    // =========================================================
    // API pública
    // =========================================================

    public static IEnumerable<Vector3> GetVisibleCorners(
        Vector3 origin,
        float viewRange,
        float cornerOffset,
        LayerMask obstacleMask)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, viewRange, _results, obstacleMask);
        float y = origin.y;

        for (int i = 0; i < count; i++)
        {
            Collider collider = _results[i];
            IEnumerable<Vector3> corners;

            switch (collider)
            {
                case BoxCollider box:
                    corners = GetBoxCorners(box, cornerOffset, y, obstacleMask);
                    break;
                case SphereCollider sphere:
                    corners = GetSphereCorners(sphere, cornerOffset, y, obstacleMask);
                    break;
                case CapsuleCollider capsule:
                    corners = GetCapsuleCorners(capsule, cornerOffset, y, obstacleMask);
                    break;
                default:
                    corners = GetMeshCorners(collider, cornerOffset, y, obstacleMask);
                    break;
            }

            foreach (Vector3 corner in corners)
            {
                if (Perception.HasLineOfSight(origin, corner, obstacleMask))
                    yield return corner;
            }
        }
    }

    public static List<Vector3> GetMergedCorners(
        IEnumerable<Vector3> points,
        float mergeDistance)
    {
        List<Vector3> merged = new();

        foreach (Vector3 point in points)
        {
            bool found = false;

            for (int i = 0; i < merged.Count; i++)
            {
                Vector3 a = merged[i]; a.y = 0f;
                Vector3 b = point; b.y = 0f;

                if ((a - b).sqrMagnitude <= mergeDistance * mergeDistance)
                {
                    merged[i] = (merged[i] + point) * 0.5f;
                    found = true;
                    break;
                }
            }

            if (!found)
                merged.Add(point);
        }

        return merged;
    }
}