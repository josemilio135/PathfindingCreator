using System.Collections.Generic;
using UnityEngine;

public static class CornerDetection
{
    static readonly Collider[] _results = new Collider[128];

    const int RADIAL_RAYS = 64;
    const float MAX_RADIUS = 256f;

    static readonly RaycastHit[] _rayHits = new RaycastHit[32];

    static Vector3 Flatten(Vector3 v, float y)
    {
        v.y = y;
        return v;
    }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;

        if (v == Vector3.zero)
            return Vector3.zero;

        return v.normalized;
    }

    static IEnumerable<Vector3> GetBoxCorners(
        BoxCollider box,
        float offset,
        float y)
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
            Vector3 worldCorner =
                t.TransformPoint(localCorners[i]);

            Vector3 dir =
                Horizontal(
                    worldCorner - t.position);

            Vector3 node =
                worldCorner + dir * offset;

            node.y = y;

            yield return node;
        }
    }

    static IEnumerable<Vector3> GetSphereCorners(
        SphereCollider sphere,
        float offset,
        float y)
    {
        Transform t = sphere.transform;

        Vector3 center =
            t.TransformPoint(
                sphere.center);

        float radius =
            sphere.radius *
            Mathf.Max(
                t.lossyScale.x,
                t.lossyScale.z);

        int points = 8;

        for (int i = 0; i < points; i++)
        {
            float angle =
                i * Mathf.PI * 2f / points;

            Vector3 localDir = new(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));

            Vector3 worldDir =
                t.TransformDirection(localDir);

            worldDir =
                Horizontal(worldDir);

            Vector3 node =
                center +
                worldDir * (radius + offset);

            node.y = y;

            yield return node;
        }
    }

    static IEnumerable<Vector3> GetCapsuleCorners(
        CapsuleCollider capsule,
        float offset,
        float y)
    {
        Transform t = capsule.transform;

        Vector3 center =
            t.TransformPoint(
                capsule.center);

        float radius =
            capsule.radius *
            Mathf.Max(
                t.lossyScale.x,
                t.lossyScale.z);

        int points = 8;

        for (int i = 0; i < points; i++)
        {
            float angle =
                i * Mathf.PI * 2f / points;

            Vector3 localDir = new(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));

            Vector3 worldDir =
                t.TransformDirection(localDir);

            worldDir =
                Horizontal(worldDir);

            Vector3 node =
                center +
                worldDir * (radius + offset);

            node.y = y;

            yield return node;
        }
    }

    static IEnumerable<Vector3> GetMeshCorners(
    Collider collider,
    float offset,
    float y)
    {
        MeshCollider meshCollider =
            collider as MeshCollider;

        // si NO es mesh collider
        // usamos el método radial viejo
        // porque para orgánicos funciona bien
        if (meshCollider == null ||
            meshCollider.sharedMesh == null)
        {
            foreach (Vector3 p in GetOrganicMeshCorners(
                collider,
                offset,
                y))
            {
                yield return p;
            }

            yield break;
        }

        Mesh mesh =
            meshCollider.sharedMesh;

        Vector3[] vertices =
            mesh.vertices;

        int[] triangles =
            mesh.triangles;

        Transform t =
            collider.transform;

        Dictionary<Vector3Int, List<Vector3>> vertexConnections =
            new();

        // =========
        // construir grafo de aristas
        // =========

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a =
                vertices[triangles[i]];

            Vector3 b =
                vertices[triangles[i + 1]];

            Vector3 c =
                vertices[triangles[i + 2]];

            RegisterEdge(a, b);
            RegisterEdge(b, c);
            RegisterEdge(c, a);
        }

        void RegisterEdge(
            Vector3 from,
            Vector3 to)
        {
            Vector3Int key =
                Quantize(from);

            if (!vertexConnections.TryGetValue(
                key,
                out List<Vector3> list))
            {
                list = new();
                vertexConnections.Add(
                    key,
                    list);
            }

            bool exists = false;

            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i] - to).sqrMagnitude < 0.0001f)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                list.Add(to);
        }

        // =========
        // detectar vertices esquina
        // =========

        List<Vector3> result = new();

        foreach (var pair in vertexConnections)
        {
            List<Vector3> connected =
                pair.Value;

            // necesitamos mínimo 2 conexiones
            if (connected.Count < 2)
                continue;

            Vector3Int key = pair.Key;

            Vector3 localVertex = new(
                key.x / 1000f,
                key.y / 1000f,
                key.z / 1000f);

            Vector3 worldVertex =
                t.TransformPoint(localVertex);

            worldVertex.y = y;

            bool isCorner = false;

            for (int i = 0; i < connected.Count; i++)
            {
                for (int j = i + 1; j < connected.Count; j++)
                {
                    Vector3 dirA =
                        Horizontal(
                            t.TransformDirection(
                                connected[i] - localVertex));

                    Vector3 dirB =
                        Horizontal(
                            t.TransformDirection(
                                connected[j] - localVertex));

                    if (dirA == Vector3.zero ||
                        dirB == Vector3.zero)
                        continue;

                    float angle =
                        Vector3.Angle(
                            dirA,
                            dirB);

                    // si NO es línea recta
                    // entonces es esquina
                    if (Mathf.Abs(angle - 180f) > 12f)
                    {
                        isCorner = true;
                        break;
                    }
                }

                if (isCorner)
                    break;
            }

            if (!isCorner)
                continue;

            Vector3 outward =
                Horizontal(
                    worldVertex -
                    collider.bounds.center);

            if (outward == Vector3.zero)
                continue;

            Vector3 node =
                worldVertex +
                outward * offset;

            node.y = y;

            result.Add(node);
        }

        // =========
        // merge final
        // =========

        result =
            Simplify(
                result,
                offset * 1.5f);

        for (int i = 0; i < result.Count; i++)
            yield return result[i];
    }

    static IEnumerable<Vector3> GetOrganicMeshCorners(
        Collider collider,
        float offset,
        float y)
    {
        Vector3 center =
            Flatten(
                collider.bounds.center,
                y);

        List<Vector3> points = new();

        for (int i = 0; i < RADIAL_RAYS; i++)
        {
            float angle =
                i * Mathf.PI * 2f / RADIAL_RAYS;

            Vector3 dir = new(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));

            dir =
                Horizontal(dir);

            Vector3 origin =
                center -
                dir * MAX_RADIUS;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    dir,
                    _rayHits,
                    MAX_RADIUS * 2f);

            float closest = float.MaxValue;
            RaycastHit selected = default;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit =
                    _rayHits[j];

                if (hit.collider != collider)
                    continue;

                if (hit.distance >= closest)
                    continue;

                closest =
                    hit.distance;

                selected = hit;
            }

            if (closest == float.MaxValue)
                continue;

            Vector3 normal =
                Horizontal(
                    selected.normal);

            if (normal == Vector3.zero)
                continue;

            Vector3 node =
                selected.point +
                normal * offset;

            node.y = y;

            points.Add(node);
        }

        points =
            Simplify(
                points,
                offset * 2f);

        for (int i = 0; i < points.Count; i++)
            yield return points[i];
    }

    static Vector3Int Quantize(
        Vector3 v)
    {
        return new Vector3Int(
            Mathf.RoundToInt(v.x * 1000f),
            Mathf.RoundToInt(v.y * 1000f),
            Mathf.RoundToInt(v.z * 1000f));
    }

    static List<Vector3> RemoveLinearPoints(
        List<Vector3> points,
        float angleThreshold)
    {
        if (points.Count <= 2)
            return points;

        List<Vector3> result = new();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 prev =
                points[(i - 1 + points.Count) % points.Count];

            Vector3 current =
                points[i];

            Vector3 next =
                points[(i + 1) % points.Count];

            Vector3 dirA =
                Horizontal(current - prev);

            Vector3 dirB =
                Horizontal(next - current);

            if (dirA == Vector3.zero ||
                dirB == Vector3.zero)
                continue;

            float angle =
                Vector3.Angle(
                    dirA,
                    dirB);

            // si casi no cambia dirección
            // es pared recta
            bool isLinear =
                angle <= angleThreshold;

            if (isLinear)
                continue;

            result.Add(current);
        }

        return result;
    }
    static List<Vector3> Simplify(
        List<Vector3> points,
        float minDistance)
    {
        List<Vector3> result = new();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 current =
                points[i];

            bool merged = false;

            for (int j = 0; j < result.Count; j++)
            {
                Vector3 a = result[j];
                a.y = 0f;

                Vector3 b = current;
                b.y = 0f;

                if ((a - b).sqrMagnitude >
                    minDistance * minDistance)
                    continue;

                merged = true;
                break;
            }

            if (!merged)
                result.Add(current);
        }

        return result;
    }

    public static IEnumerable<Vector3> GetVisibleCorners(
        Vector3 origin,
        float viewRange,
        float cornerOffset,
        LayerMask obstacleMask)
    {
        int count =
            Physics.OverlapSphereNonAlloc(
                origin,
                viewRange,
                _results,
                obstacleMask);

        float y = origin.y;

        for (int i = 0; i < count; i++)
        {
            Collider collider =
                _results[i];

            IEnumerable<Vector3> corners = null;

            switch (collider)
            {
                case BoxCollider box:
                    corners =
                        GetBoxCorners(
                            box,
                            cornerOffset,
                            y);
                    break;

                case SphereCollider sphere:
                    corners =
                        GetSphereCorners(
                            sphere,
                            cornerOffset,
                            y);
                    break;

                case CapsuleCollider capsule:
                    corners =
                        GetCapsuleCorners(
                            capsule,
                            cornerOffset,
                            y);
                    break;

                default:
                    corners =
                        GetMeshCorners(
                            collider,
                            cornerOffset,
                            y);
                    break;
            }

            foreach (Vector3 corner in corners)
            {
                bool canSee =
                    Perception.HasLineOfSight(
                        origin,
                        corner,
                        obstacleMask);

                if (!canSee)
                    continue;

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
                Vector3 a = merged[i];
                a.y = 0f;

                Vector3 b = point;
                b.y = 0f;

                if ((a - b).sqrMagnitude >
                    mergeDistance * mergeDistance)
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