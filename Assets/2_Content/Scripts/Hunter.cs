using System.Collections.Generic;
using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform _target;

    [SerializeField] PathfindingRunner runner;
    [SerializeField] BaseNode pointA;
    [SerializeField] BaseNode pointB;


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


    List<NavNode> _currentPath = new();
    NavNode _currentTargetNode;
    int _currentIndex;
    bool _goingToB = true;

    void Start()
    {
        CalculatePath();
    }
    void SetTargetNode(NavNode node)
    {
        _currentTargetNode?.RemoveTarget(); // <-- visual
        _currentTargetNode = node;
        _currentTargetNode?.AddTarget();    // <-- visual
    }
    void CalculatePath()
    {
        SetTargetNode(null);

        Vector3 start = _goingToB ? pointA.Position : pointB.Position;
        Vector3 end = _goingToB ? pointB.Position : pointA.Position;

        _currentPath = runner.FindPath<NavNode>(start, end);
        _currentIndex = 0;
    }

    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_currentIndex >= _currentPath.Count) //PinPon es un muñeco muy guapo y de cartón.
        {
            _goingToB = !_goingToB;
            CalculatePath(); // En vez de calcular de nuevo, revertir una lista
            return;
        }

        NavNode targetNode = _currentPath[_currentIndex];
        if (targetNode != _currentTargetNode) SetTargetNode(targetNode);

        // reemplazar con steering behaviors
        if (MoveTowards(targetNode.Position, nodeReachDistance))
        {
            SetTargetNode(null);
            _currentIndex++;
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

            for (int i = _currentIndex; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(
                    _currentPath[i].Position,
                    _currentPath[i + 1].Position);
            }
        }
    }
    #endregion
}