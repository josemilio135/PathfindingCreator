using System.Collections.Generic;
using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform _target;

    [Header("Patrol")]
    [SerializeField] PathfindingRunner _runner;
    [SerializeField] Transform _waypointsRoot;
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

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    bool _walkingToWaypoint = false;

    List<NavNode> _currentPath = new();
    List<Transform> _waypoints = new();
    NavNode _currentTargetNode;
    int _currentNodeIndex;
    int _waypointIndex = 0;
    int _waypointDir = 1;

    void Start()
    {
        BuildWaypoints();
        if (_waypoints.Count > 0) CalculatePath();
    }
    void BuildWaypoints()
    {
        _waypoints.Clear();
        if (_waypointsRoot == null) return;
        foreach (Transform child in _waypointsRoot) _waypoints.Add(child);
    }
    void SetTargetNode(NavNode node)
    {
        _currentTargetNode?.RemoveTarget(); // <-- visual
        _currentTargetNode = node;
        _currentTargetNode?.AddTarget();    // <-- visual
    }
    void CalculatePath()
    {
        if (_waypoints.Count == 0) return;

        SetTargetNode(null);
        _walkingToWaypoint = false;

        Vector3 start = transform.position;
        Vector3 end = _waypoints[_waypointIndex].position;

        _currentPath = _runner.FindPath<NavNode>(start, end);
        _currentNodeIndex = 0;
    }
    void NextWaypoint()
    {
        if (_pingPong)
        {
            _waypointIndex += _waypointDir;

            if (_waypointIndex >= _waypoints.Count)
            {
                _waypointDir = -1;
                _waypointIndex = _waypoints.Count - 2;
            }
            else if (_waypointIndex < 0)
            {
                _waypointDir = 1;
                _waypointIndex = 1;
            }
        }
        else _waypointIndex = (_waypointIndex + 1) % _waypoints.Count;

        CalculatePath();
    }
    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_walkingToWaypoint)
        {
            Vector3 waypointPos = _waypoints[_waypointIndex].position;
            if (MoveTowards(waypointPos, nodeReachDistance))
            {
                _walkingToWaypoint = false;
                NextWaypoint();
            }
            return;
        }

        if (_currentNodeIndex >= _currentPath.Count)
        {
            _walkingToWaypoint = true;
            return;
        }

        NavNode targetNode = _currentPath[_currentNodeIndex];
        if (targetNode != _currentTargetNode) SetTargetNode(targetNode);

        if (MoveTowards(targetNode.Position, nodeReachDistance))
        {
            SetTargetNode(null);
            _currentNodeIndex++;
        }
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
        float distance = Vector3.Distance(transform.position, target);
        if (distance <= stoppingDistance) return true;

        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        return false;
    }

    void OnDestroy()
    {
        SetTargetNode(null);
    }
    #region Gizmos
    void OnDrawGizmos()
    {
        Vector3 eyesPosition =
            transform.position + _eyesOffset;

        if (_drawVisionRange)
        {
            Gizmos.color = _isInRange ? _rangeColor : Color.gray;

            Gizmos.DrawWireSphere(
                eyesPosition, _viewRange);

            Vector3 leftBoundary =
                Quaternion.Euler(
                    0f, -_fovAngle * 0.5f, 0f) * transform.forward;

            Vector3 rightBoundary =
                Quaternion.Euler(
                    0f, _fovAngle * 0.5f, 0f) * transform.forward;

            Gizmos.color =
                _isInsideAngle ? _viewAngleColor : Color.gray;

            Gizmos.DrawRay(
                eyesPosition, leftBoundary * _viewRange);

            Gizmos.DrawRay(
                eyesPosition, rightBoundary * _viewRange);
        }

        if (_drawVisionRay && _target != null)
        {
            Vector3 targetPosition =
                _target.position + _eyesOffset;

            Gizmos.color =
                _canSeeTarget ? _lodColor : Color.gray;

            Gizmos.DrawLine(
                eyesPosition, targetPosition);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(eyesPosition, 0.1f);


        if (_drawPath && _currentPath != null && _currentPath.Count > 1)
        {
            Gizmos.color = _pathColor;
            for (int i = _currentNodeIndex; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_currentPath[i].Position, _currentPath[i + 1].Position);
            }
        }
    }
    #endregion
}