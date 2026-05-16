using UnityEngine;

public class NodeViewer : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] LayerMask _wallMask;
    [SerializeField, Min(0f)] float _viewRange = 15f;

    [Header("Corners")]
    [SerializeField, Min(0f)] float _cornerOffset = .25f;

    [Header("Debug")]

    readonly Collider[] _results = new Collider[64];

    //
    public LayerMask WallMask => _wallMask;
    public float ViewRange => _viewRange;
    public float CornerOffset => _cornerOffset;
    public Collider[] Results => _results;
    public bool IsClean { get; private set; } = true;


    public void BakeCorners()
    {
        IsClean = false;
        Debug.Log("Empezando bake");
    }
    public void ClearCorners()
    {
        IsClean = true;
        Debug.Log("x cantidad de nodos borrados");
    }


    public int ScanWalls()
    {
        // Detect colliders and store them in the results array.
         return Physics.OverlapSphereNonAlloc(
            transform.position, _viewRange, _results, _wallMask);
    }
    public Vector3[] GetCorners(BoxCollider box)
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

        Vector3[] worldCorners = new Vector3[4];

        for (int i = 0; i < localCorners.Length; i++)
        {
            worldCorners[i] = t.TransformPoint(localCorners[i]);
        }

        return worldCorners;
    }

}