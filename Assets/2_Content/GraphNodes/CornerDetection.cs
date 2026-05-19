using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class CornerDetection
{
    // ─────────────────────────────────────────────
    // Buffers estáticos (evitan allocs en runtime)
    // ─────────────────────────────────────────────

    static readonly Collider[] _overlapResults = new Collider[128];
    static readonly RaycastHit[] _rayHits = new RaycastHit[32];
    static readonly Collider[] _clearanceHits = new Collider[8];

    const int RADIAL_RAYS = 64;
    const float MAX_RADIUS = 256f;
    const int ORGANIC_VERTEX_THRESHOLD = 32;
    const float CORNER_ANGLE_THRESHOLD = 12f;

    // ─────────────────────────────────────────────
    // Helpers geométricos
    // ─────────────────────────────────────────────

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v == Vector3.zero ? Vector3.zero : v.normalized;
    }

    // Snap al suelo: lanza un rayo hacia abajo desde el candidato.
    // Ignora el collider del obstáculo que lo generó para no chocar con él.
    // Si no encuentra suelo, usa la Y del generador (fallbackY).
    static Vector3 SnapToGround(
    Vector3 nodePoint,
    float agentHeight,
    float agentRadius,
    Collider sourceCollider,
    LayerMask obstacleMask)
    {
        Vector3 rayOrigin =
            nodePoint + Vector3.up * (agentHeight + 1f);

        int hitCount = Physics.RaycastNonAlloc(
            rayOrigin,
            Vector3.down,
            _rayHits,
            agentHeight + 10f,
            ~0,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        float bestY = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _rayHits[i];

            if (hit.collider == sourceCollider)
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.6f)
                continue;

            Vector3 checkPoint = hit.point;

            Vector3 bottom =
                checkPoint + Vector3.up * agentRadius;

            Vector3 top =
                checkPoint + Vector3.up * (agentHeight - agentRadius);

            int overlaps = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                agentRadius,
                _clearanceHits,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            bool blocked = false;

            for (int j = 0; j < overlaps; j++)
            {
                Collider c = _clearanceHits[j];

                if (c == sourceCollider)
                    continue;

                if (c == hit.collider)
                    continue;

                blocked = true;
                break;
            }

            if (blocked)
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        nodePoint.y = found
            ? bestY
            : sourceCollider.bounds.min.y;

        return nodePoint;
    }

    // Verifica que el agente quepa en el punto (sin chocar con obstáculos).
    // Hace un OverlapCapsule con el radio mínimo del agente.
    // El collider fuente se ignora (es el obstáculo mismo, no cuenta).
    

    // Altura del ecuador más ancho del collider (para la roca-huevo).
    // Samplea en varias alturas y elige la que tiene mayor radio XZ real.
    static float FindWidestY(Collider collider, int samples = 8)
    {
        Bounds b = collider.bounds;
        float bestY = b.center.y;
        float bestR = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (i + 0.5f) / samples;
            float sampleY = Mathf.Lerp(b.min.y, b.max.y, t);
            Vector3 ctr = new(b.center.x, sampleY, b.center.z);

            // Raycast lateral para medir radio real en esta altura
            Vector3 testOrigin = ctr + Vector3.right * MAX_RADIUS;
            float r = b.extents.x; // fallback si no pega

            if (Physics.Raycast(testOrigin, Vector3.left, out RaycastHit hit, MAX_RADIUS * 2f)
                && hit.collider == collider)
            {
                r = Mathf.Abs(hit.point.x - ctr.x);
            }

            if (r > bestR) { bestR = r; bestY = sampleY; }
        }

        return bestY;
    }

    // ─────────────────────────────────────────────
    // Primitivos — cada uno con su lógica exacta
    // ─────────────────────────────────────────────

    static IEnumerable<Vector3> GetBoxCorners(
        BoxCollider box, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        Transform t = box.transform;
        Vector3 ctr = box.center;
        Vector3 half = box.size * 0.5f;

        Vector3[] localCorners =
        {
            ctr + new Vector3( half.x, 0f,  half.z),
            ctr + new Vector3( half.x, 0f, -half.z),
            ctr + new Vector3(-half.x, 0f,  half.z),
            ctr + new Vector3(-half.x, 0f, -half.z),
        };

        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector3 worldCorner = t.TransformPoint(localCorners[i]);
            Vector3 dir = Horizontal(worldCorner - t.TransformPoint(ctr));
            if (dir == Vector3.zero) continue;

            Vector3 candidate = worldCorner + dir * totalOffset;
            candidate = SnapToGround(
     candidate,
     agentHeight,
     agentRadius,
     box,
     obstacleMask);

                yield return candidate;
        }
    }

    static IEnumerable<Vector3> GetSphereCorners(
        SphereCollider sphere, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        Transform t = sphere.transform;
        Vector3 center = t.TransformPoint(sphere.center);
        float radius = sphere.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8;
            Vector3 dir = Horizontal(t.TransformDirection(
                                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))));

            Vector3 candidate = center + dir * (radius + totalOffset);
            candidate = candidate = SnapToGround(
    candidate,
    agentHeight,
    agentRadius,
    sphere,
    obstacleMask);

                yield return candidate;
        }
    }

    static IEnumerable<Vector3> GetCapsuleCorners(
        CapsuleCollider capsule, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center);
        float radius = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8;
            Vector3 dir = Horizontal(t.TransformDirection(
                                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))));

            Vector3 candidate = center + dir * (radius + totalOffset);
            candidate = candidate = SnapToGround(
    candidate,
    agentHeight,
    agentRadius,
    capsule,
    obstacleMask);

                yield return candidate;
        }
    }

    // ─────────────────────────────────────────────
    // MeshCollider — dispatcher geométrico / orgánico
    // ─────────────────────────────────────────────

    static IEnumerable<Vector3> GetMeshCorners(
        Collider collider, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        // Si no es MeshCollider (no debería llegar aquí, pero por seguridad)
        // o no tiene mesh → orgánico
        if (collider is not MeshCollider mc || mc.sharedMesh == null)
        {
            foreach (var p in GetOrganicMeshCorners(
                collider, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask))
                yield return p;
            yield break;
        }

        int unique = CountUniqueXZVertices(mc.sharedMesh, mc.transform);

        IEnumerable<Vector3> source = unique <= ORGANIC_VERTEX_THRESHOLD
            ? GetGeometricMeshCorners(mc, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask)
            : GetOrganicMeshCorners(mc, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask);

        foreach (var p in source)
            yield return p;
    }

    // MeshCollider geométrico: análisis de vértices (ProBuilder, paredes)
    static IEnumerable<Vector3> GetGeometricMeshCorners(
        MeshCollider mc, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        Mesh mesh = mc.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Transform t = mc.transform;

        // Fusionar vértices duplicados de ProBuilder por posición mundo XZ
        var worldPosMap = new Dictionary<Vector3Int, Vector3>();
        var vertexKeys = new Vector3Int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 world = t.TransformPoint(vertices[i]);
            Vector3Int key = QuantizeXZ(world);
            vertexKeys[i] = key;
            if (!worldPosMap.ContainsKey(key))
                worldPosMap[key] = world;
        }

        // Grafo de adyacencia en planta
        var adjacency = new Dictionary<Vector3Int, HashSet<Vector3Int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(adjacency, vertexKeys[triangles[i]], vertexKeys[triangles[i + 1]]);
            AddEdge(adjacency, vertexKeys[triangles[i + 1]], vertexKeys[triangles[i + 2]]);
            AddEdge(adjacency, vertexKeys[triangles[i + 2]], vertexKeys[triangles[i]]);
        }

        var candidates = new List<Vector3>();
        Vector3 boundsCenter = mc.bounds.center;

        foreach (var pair in adjacency)
        {
            if (!worldPosMap.TryGetValue(pair.Key, out Vector3 worldVertex)) continue;
            if (pair.Value.Count < 2) continue;

            var neighborDirs = new List<Vector3>();

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

            Vector3 candidate = worldVertex + outward * totalOffset;
            candidate = candidate = SnapToGround(
    candidate,
    agentHeight,
    agentRadius,
    mc,
    obstacleMask); ;

                candidates.Add(candidate);
        }

        candidates = Simplify(candidates, totalOffset * 1.5f);
        foreach (var p in candidates) yield return p;
    }

    // MeshCollider orgánico: raycast radial desde el ecuador más ancho
    static IEnumerable<Vector3> GetOrganicMeshCorners(
        Collider collider, float totalOffset,
        float agentHeight, float agentRadius,
        float fallbackY, LayerMask obstacleMask)
    {
        // Sampleamos a la altura más ancha del obstáculo (resuelve el huevo)
        float widestY = FindWidestY(collider);
        Vector3 center = collider.bounds.center;
        center.y = widestY;

        var points = new List<Vector3>();

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

            Vector3 candidate = selected.point + normal * totalOffset;
            candidate = candidate = SnapToGround(
    candidate,
    agentHeight,
    agentRadius,
    collider,
    obstacleMask);

                points.Add(candidate);
        }

        points = Simplify(points, totalOffset * 2f);
        foreach (var p in points) yield return p;
    }

    // ─────────────────────────────────────────────
    // Utilidades internas
    // ─────────────────────────────────────────────

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

    static int CountUniqueXZVertices(Mesh mesh, Transform t)
    {
        var seen = new HashSet<Vector3Int>();
        foreach (Vector3 v in mesh.vertices)
            seen.Add(QuantizeXZ(t.TransformPoint(v)));
        return seen.Count;
    }

    static Vector3Int QuantizeXZ(Vector3 v) =>
        new(Mathf.RoundToInt(v.x * 1000f), 0, Mathf.RoundToInt(v.z * 1000f));

    static List<Vector3> Simplify(List<Vector3> points, float minDist)
    {
        var result = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 cur = points[i];
            bool skip = false;

            for (int j = 0; j < result.Count; j++)
            {
                Vector3 a = result[j]; a.y = 0f;
                Vector3 b = cur; b.y = 0f;
                if ((a - b).sqrMagnitude <= minDist * minDist) { skip = true; break; }
            }

            if (!skip) result.Add(cur);
        }

        return result;
    }

    public static IEnumerable<Vector3> GetVisibleCorners(
        Vector3 origin,
        float viewRange,
        float agentRadius,
       
        float agentHeight,
        LayerMask obstacleMask)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, viewRange, _overlapResults, obstacleMask);
        float totalOffset = agentRadius;
        float fallbackY = origin.y;

        for (int i = 0; i < count; i++)
        {
            Collider collider = _overlapResults[i];
            IEnumerable<Vector3> corners;

            switch (collider)
            {
                case BoxCollider box:
                    corners = GetBoxCorners(box, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask);
                    break;

                case SphereCollider sphere:
                    corners = GetSphereCorners(sphere, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask);
                    break;

                case CapsuleCollider capsule:
                    corners = GetCapsuleCorners(capsule, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask);
                    break;

                default:
                    corners = GetMeshCorners(collider, totalOffset, agentHeight, agentRadius, fallbackY, obstacleMask);
                    break;
            }

            foreach (Vector3 corner in corners)
            {
                if (Perception.HasLineOfSight(origin, corner, obstacleMask))
                    yield return corner;
            }
        }
    }

    /// <summary>
    /// Fusiona puntos cercanos promediando su posición.
    /// </summary>
    public static List<Vector3> GetMergedCorners(IEnumerable<Vector3> points, float mergeDistance)
    {
        var merged = new List<Vector3>();

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

            if (!found) merged.Add(point);
        }

        return merged;
    }
}