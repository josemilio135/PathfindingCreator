using UnityEngine;

[System.Serializable]
public class VisionFOV
{
    [SerializeField] FOV_Mesh _fovMesh;
    [Space]
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);
    [Space]
    [SerializeField] LayerMask _agentMask;
    [SerializeField] LayerMask _obstacleMask;
    public float ViewRange => _viewRange;

    readonly Collider[] _hits = new Collider[32];

    public void Initialize()
    {
        if (_fovMesh)
        {
            _fovMesh.SetConfig(_viewRange, _fovAngle, _obstacleMask, _eyesOffset);
        }
    }
    public bool FindEnemy(Transform origin, Teamates team, ref IHaveTeamate currentTarget)
    {
        if (currentTarget != null)
        {
            if (CanSeeTarget(origin, currentTarget)) return true;
            currentTarget = null;
        }

        Vector3 eyes = origin.position + _eyesOffset;

        int count = Physics.OverlapSphereNonAlloc(
            eyes, _viewRange, _hits, _agentMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!_hits[i].TryGetComponentInParent<IHaveTeamate>(out var target)) continue;
            if (target.Team == team) continue;
            if (!CanSeeTarget(origin, target)) continue;

            currentTarget = target;

            return true;
        }
        return false;
    }
    public bool CanSeeTarget(Transform origin, IHaveTeamate target)
    {
        Vector3 eyes = origin.position + _eyesOffset;
        Vector3 targetPos = target.Position + _eyesOffset;

        if (!Perception.IsInRange(eyes, targetPos, _viewRange)) return false;
        if (!Perception.IsInViewAngle(eyes, origin.forward, targetPos, _fovAngle)) return false;
        if (!Perception.HasLineOfSight(eyes, targetPos, _obstacleMask)) return false;

        return true;
    }

    public void SetFovColor(string hexadecimal, float alpha = .2f)
    {
        if (_fovMesh == null) return;
        ColorUtility.TryParseHtmlString("#" + hexadecimal, out Color color);
        color.a = alpha;
        _fovMesh.SetColor(color);
    }
    public void Refresh()
    {
        if (_fovMesh != null)
        {
            _fovMesh.SetConfig(_viewRange, _fovAngle, _obstacleMask, _eyesOffset);
        }
    }
}