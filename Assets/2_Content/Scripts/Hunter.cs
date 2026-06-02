using System.Collections.Generic;
using UnityEngine;

public class Hunter : AgentRunner
{
    [Header("Patrol")]
    [SerializeField] Transform _waypointsContainer;
    [SerializeField] bool _pingPong = true;

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] bool _drawVision = true;
    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    Transform[] _waypoints;
    int _waypointIndex;
    int _waypointDir = 1;

    protected override void Awake()
    {
        base.Awake();

        if (_waypointsContainer != null)
        {
            _waypoints = new Transform[_waypointsContainer.childCount];
            for (int i = 0; i < _waypointsContainer.childCount; i++)
                _waypoints[i] = _waypointsContainer.GetChild(i);
        }
        else _waypoints = System.Array.Empty<Transform>();
    }

    void Start()
    {
        if (_waypoints.Length > 0) GoToCurrentWaypoint();
    }

    void Update()
    {
        FollowPath();
        // if (EvaluateVision()) ChaseTarget();
    }

    protected override void OnDestinationReached() => NextWaypoint();

    void GoToCurrentWaypoint()
    {
        if (_waypoints.Length == 0) return;
        SetDestination(_waypoints[_waypointIndex].position);
    }

    void NextWaypoint()
    {
        if (_pingPong)
        {
            _waypointIndex += _waypointDir;

            if (_waypointIndex >= _waypoints.Length)
            {
                _waypointDir = -1;
                _waypointIndex = _waypoints.Length - 2;
            }
            else if (_waypointIndex < 0)
            {
                _waypointDir = 1;
                _waypointIndex = 1;
            }
        }
        else _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;

        GoToCurrentWaypoint();
    }

    void ChaseTarget(Transform target)
    {
        if (target == null) return;
        SetDestination(target.position);
    }

    bool EvaluateVision(Transform target)
    {
        if (target == null) return false;

        Vector3 eyes = transform.position + _eyesOffset;
        Vector3 targetPos = target.position + _eyesOffset;

        _isInRange = Perception.IsInRange(eyes, targetPos, _viewRange);
        if (!_isInRange) return false;

        _isInsideAngle = Perception.IsInViewAngle(eyes, transform.forward, targetPos, _fovAngle);
        if (!_isInsideAngle) return false;

        _canSeeTarget = Perception.HasLineOfSight(eyes, targetPos, _obstacleMask);
        return _canSeeTarget;
    }

    protected override void DrawGizmos(List<BaseNode> path, int currentIndex)
    {
        // Vision
        if (!_drawVision) return;

        Vector3 eyes = transform.position + _eyesOffset;

        Gizmos.color = _isInRange ? _rangeColor : Color.gray;
        Gizmos.DrawWireSphere(eyes, _viewRange);

        Gizmos.color = _isInsideAngle ? _viewAngleColor : Color.gray;
        Gizmos.DrawRay(eyes, Quaternion.Euler(0f, -_fovAngle * 0.5f, 0f) * transform.forward * _viewRange);
        Gizmos.DrawRay(eyes, Quaternion.Euler(0f, _fovAngle * 0.5f, 0f) * transform.forward * _viewRange);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(eyes, 0.1f);
    }
}