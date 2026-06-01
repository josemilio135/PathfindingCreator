using System.Collections.Generic;
using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform _target;

    [Header("Patrol")]
    [SerializeField] PathfindingRunner _runner;
    [SerializeField] WaypointNode[] _waypoints;
    [SerializeField] bool _pingPong = true;


    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] float nodeReachDistance = 0.2f;

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;

    [Header("Offsets")]
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);
    [SerializeField] float _stoppingDistance = .5f;


    [Header("Debug")]
    [SerializeField] bool _drawVisionRay = true;
    [SerializeField] bool _drawVisionRange = true;
    [SerializeField] bool _drawPath = true;

    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;
    [SerializeField] Color _lodColor = Color.green;
    [SerializeField] Color _pathColor = Color.white;
    [SerializeField] Color _targetNodeColor = Color.blue;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    List<BaseNode> _currentPath = new();
    int _currentIndex;
    int _waypointIndex = 0;
    int _waypointDir = 1;

    void Start()
    {
        if (_waypoints.Length > 0) CalculatePath();
    }

    void CalculatePath()
    {
        if (_waypoints.Length == 0) return;

        WaypointNode destination = _waypoints[_waypointIndex];

        destination.Connect(_runner.Container);
        _currentPath = _runner.FindPath<BaseNode>(transform.position, destination.Position);
        _currentIndex = 0;
    }

    public void SetDestination(Vector3 destination)
    {
        _currentPath = _runner.FindPath<BaseNode>(transform.position, destination);
        _currentIndex = 0;
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

        CalculatePath();
    }

    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_currentIndex >= _currentPath.Count)
        {
            NextWaypoint();
            return;
        }

        if (MoveTowards(_currentPath[_currentIndex].Position, nodeReachDistance))
            _currentIndex++;
    }

    void Update()
    {
        FollowPath();
        // if (EvaluateVision()) ChaseTarget();
    }
    void ChaseTarget()
    {
        if (_target == null) return;
        MoveTowards(_target.position, _stoppingDistance);
    }
    bool EvaluateVision()
    {
        if (_target == null) return false;

        Vector3 eyesPosition = transform.position + _eyesOffset;
        Vector3 targetPosition = _target.position + _eyesOffset;

        _isInRange =
            Perception.IsInRange(eyesPosition, targetPosition, _viewRange);
        if (!_isInRange) return false;

        _isInsideAngle =
            Perception.IsInViewAngle(eyesPosition, transform.forward, targetPosition, _fovAngle);
        if (!_isInsideAngle) return false;

        _canSeeTarget =
            Perception.HasLineOfSight(eyesPosition, targetPosition, _obstacleMask);
        if (!_canSeeTarget) return false;

        return true;
    }
    bool MoveTowards(Vector3 target, float stoppingDistance)
    {
        Vector3 flat = new(target.x, transform.position.y, target.z);
        float distance = Vector3.Distance(transform.position, flat);
        if (distance <= stoppingDistance) return true;

        Vector3 direction = (flat - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        return false;
    }


    #region Gizmos
    void OnDrawGizmos()
    {
        Vector3 eyes = transform.position + _eyesOffset;

        if (_drawVisionRange)
        {
            Gizmos.color = _isInRange ? _rangeColor : Color.gray;
            Gizmos.DrawWireSphere(eyes, _viewRange);

            Vector3 left = Quaternion.Euler(0f, -_fovAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, _fovAngle * 0.5f, 0f) * transform.forward;
            Gizmos.color = _isInsideAngle ? _viewAngleColor : Color.gray;
            Gizmos.DrawRay(eyes, left * _viewRange);
            Gizmos.DrawRay(eyes, right * _viewRange);
        }

        if (_drawVisionRay && _target != null)
        {
            Gizmos.color = _canSeeTarget ? _lodColor : Color.gray;
            Gizmos.DrawLine(eyes, _target.position + _eyesOffset);
        }

        //Pathfinding
        if (_drawPath && _currentPath != null && _currentPath.Count > 0)
        {
            Gizmos.color = _pathColor;
            for (int i = 0; i < _currentPath.Count - 1; i++)
                Gizmos.DrawLine(_currentPath[i].Position, _currentPath[i + 1].Position);

            for (int i = 0; i < _currentPath.Count; i++)
                Gizmos.DrawSphere(_currentPath[i].Position, 0.25f);

            if (_currentIndex < _currentPath.Count)
            {
                Gizmos.color = _targetNodeColor;
                Gizmos.DrawSphere(_currentPath[_currentIndex].Position, 0.5f);
            }
        }

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(eyes, 0.1f);
    }
    #endregion
}