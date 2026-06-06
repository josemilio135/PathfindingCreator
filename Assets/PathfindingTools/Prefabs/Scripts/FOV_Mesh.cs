using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FOV_Mesh : MonoBehaviour
{
    [SerializeField, Min(1)] int _arcSegments = 8;
    [SerializeField, Min(0f)] float _distance = 10f;
    [SerializeField, Range(0f, 360f)] float _angle = 90f;
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] int _edgeIterations = 4;
    [SerializeField, Min(0f)] float _edgeThreshold = 0.5f;

    MeshFilter _meshFilter;
    Mesh _mesh;

    struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;
    }

    void Awake()
    {
        Init();
    }

    void OnValidate()
    {
        Init();
        UpdateMesh();
    }

    void Init()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_mesh == null) 
        {
            _mesh = new Mesh();
            _meshFilter.mesh = _mesh; 
        }
    }

    public void SetConfig(float distance, float angle, LayerMask obstacleMask)
    {
        _distance = distance;
        _angle = angle;
        _obstacleMask = obstacleMask;
    }

    public void UpdateMesh()
    {
        float halfAngle = _angle * 0.5f;
        float angleStep = _angle / _arcSegments;

        List<Vector3> viewPoints = new();
        ViewCastInfo prevCast = default;

        for (int i = 0; i <= _arcSegments; i++)
        {
            float globalAngle = transform.eulerAngles.y - halfAngle + angleStep * i;
            ViewCastInfo cast = ViewCast(globalAngle);

            if (i > 0)
            {
                bool edgeDstExceeded = Mathf.Abs(prevCast.dst - cast.dst) > _edgeThreshold;
                if (prevCast.hit != cast.hit || (prevCast.hit && cast.hit && edgeDstExceeded))
                {
                    EdgeInfo edge = FindEdge(prevCast, cast);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(cast.point);
            prevCast = cast;
        }

        int count = viewPoints.Count;
        Vector3[] vertices = new Vector3[count + 1];
        int[] triangles = new int[(count - 1) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < count; i++)
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

        for (int i = 0; i < count - 1; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle);
        return Physics.Raycast(transform.position, dir, out RaycastHit hit, _distance, _obstacleMask)
            ? new ViewCastInfo { hit = true, point = hit.point, dst = hit.distance, angle = globalAngle }
            : new ViewCastInfo { hit = false, point = transform.position + dir * _distance, dst = _distance, angle = globalAngle };
    }

    EdgeInfo FindEdge(ViewCastInfo minCast, ViewCastInfo maxCast)
    {
        float minAngle = minCast.angle;
        float maxAngle = maxCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < _edgeIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo newCast = ViewCast(angle);
            bool exceeded = Mathf.Abs(minCast.dst - newCast.dst) > _edgeThreshold;

            if (newCast.hit == minCast.hit && !exceeded)
            { minAngle = angle; minPoint = newCast.point; }
            else
            { maxAngle = angle; maxPoint = newCast.point; }
        }

        return new EdgeInfo { pointA = minPoint, pointB = maxPoint };
    }

    Vector3 DirFromAngle(float globalAngle)
    {
        return new Vector3(
            Mathf.Sin(globalAngle * Mathf.Deg2Rad),
            0f
            , Mathf.Cos(globalAngle * Mathf.Deg2Rad));
    }

    struct EdgeInfo { public Vector3 pointA, pointB; }
}