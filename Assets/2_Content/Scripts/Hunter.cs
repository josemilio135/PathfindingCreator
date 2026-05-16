using UnityEngine;

public class Hunter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform _target;

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;

    [Header("Offsets")]
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] bool _drawVisionRay = true;
    [SerializeField] bool _drawVisionRange = true;

    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;
    [SerializeField] Color _lodColor = Color.green;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    void Update()
    {
        EvaluateVision();
    }

    void EvaluateVision()
    {
        _isInRange = false;
        _isInsideAngle = false;
        _canSeeTarget = false;

        if (_target == null)
            return;

        Vector3 eyesPosition =
            transform.position + _eyesOffset;

        Vector3 targetPosition =
            _target.position + _eyesOffset;

        _isInRange =
            Perception.IsInRange(eyesPosition, targetPosition, _viewRange);

        if (!_isInRange)
            return;

        _isInsideAngle =
            Perception.IsInViewAngle(
                eyesPosition, transform.forward, targetPosition, _fovAngle);

        if (!_isInsideAngle)
            return;

        _canSeeTarget =
            Perception.HasLineOfSight(
                eyesPosition, targetPosition, _obstacleMask);
    }

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
}