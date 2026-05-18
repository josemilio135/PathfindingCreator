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
        Transform t = collider.transform;

        // IMPORTANTE:
        // antes usabas t.position y eso rompe muchísimo en ProBuilder,
        // porque el pivot puede estar lejos o mal centrado.
        // bounds.center es MUCHO más estable.

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

            // arrancamos lejos y disparamos hacia el objeto
            Vector3 origin =
                center -
                dir * MAX_RADIUS;

            origin.y = y;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    dir,
                    _rayHits,
                    MAX_RADIUS * 2f);

            if (hitCount == 0)
                continue;

            // queremos el PRIMER HIT del collider
            // NO el más lejano
            // porque ProBuilder genera caras internas/raras
            // y el farthest rompe todo

            float closestDistance = float.MaxValue;
            RaycastHit selected = default;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit =
                    _rayHits[j];

                if (hit.collider != collider)
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance =
                    hit.distance;

                selected = hit;
            }

            if (closestDistance == float.MaxValue)
                continue;

            Vector3 normal =
                Horizontal(
                    selected.normal);

            // algunas caras de probuilder devuelven normales basura
            if (normal == Vector3.zero)
            {
                normal =
                    Horizontal(
                        selected.point - center);
            }

            // ESTE OFFSET ES EL IMPORTANTE
            // aleja el nodo de la pared
            Vector3 node =
                selected.point +
                normal * offset;

            node.y = y;

            points.Add(node);
        }

        // merge suave
        // no agresivo porque destruye esquinas
        List<Vector3> simplified =
            Simplify(
                points,
                offset);

        // quitar puntos alineados
        simplified =
            RemoveLinearPoints(
                simplified,
                8f);

        for (int i = 0; i < simplified.Count; i++)
            yield return simplified[i];
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