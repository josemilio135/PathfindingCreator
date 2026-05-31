using System.Collections.Generic;
using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform _target;

    [SerializeField] PathfindingRunner runner;
    [SerializeField] NavNode pointA;
    [SerializeField] NavNode pointB;


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

    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;
    [SerializeField] Color _lodColor = Color.green;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;


    List<INode> currentPath = new();

    int currentIndex;
    bool goingToB = true;

    void Start()
    {
        CalculatePath();
    }

    void CalculatePath()
    {
        Vector3 start =
            goingToB
                ? pointA.Position
                : pointB.Position;

        Vector3 end =
            goingToB
                ? pointB.Position
                : pointA.Position;

        currentPath =
            runner.FindPath(start, end);

        currentIndex = 0;
    }

    void FollowPath()
    {
        if (currentPath == null)
            return;

        if (currentPath.Count == 0)
            return;

        if (currentIndex >= currentPath.Count)
        {
            goingToB = !goingToB;

            CalculatePath();
            return;
        }

        Vector3 targetPosition =
            currentPath[currentIndex].Position;

        float distance =
            Vector3.Distance(
                transform.position,
                targetPosition);

        if (distance <= nodeReachDistance)
        {
            currentIndex++;
            return;
        }

        Vector3 direction =
            (targetPosition - transform.position).normalized;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
        }

        transform.position +=
            transform.forward *
            moveSpeed *
            Time.deltaTime;
    }

    void Update()
    {
        FollowPath();
        //        if (EvaluateVision()) ChaseTarget();
    }
    void ChaseTarget()
    {
        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= _stoppingDistance) return;

        Vector3 direction = (_target.position - transform.position);
        direction.Normalize();
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            var lookRot = Quaternion.LookRotation(direction);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
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
    }
    #endregion
}