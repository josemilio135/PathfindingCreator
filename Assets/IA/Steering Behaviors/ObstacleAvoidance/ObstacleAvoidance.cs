using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class ObstacleAvoidanceSteering : MonoBehaviour
{
    public enum AvoidMode
    {
        Whiskers,
        Predictive
    }

    const float VelocityThreshold = .01f;
    const float GroundOffset = .05f;
    const float HitGizmoRadius = .08f;
    const float AvoidVectorScale = .35f;

    [Header("General")]

    [Tooltip("Importance compared to other steering.")]
    [SerializeField, Range(0f, 10f)] float _weight = 1.5f;

    [Tooltip("Obstacle avoidance method.")]
    [SerializeField] AvoidMode _mode = AvoidMode.Predictive;

    [Tooltip("Layers treated as obstacles.")]
    [SerializeField] LayerMask _obstacleMask;

    [Tooltip("Automatically scales look ahead using current speed.")]
    [SerializeField] bool _automaticLookAhead = true;

    [Tooltip("Detection distance.")]
    [SerializeField, Min(.1f)] float _lookAhead = 3f;

    [Header("Whiskers")]

    [Tooltip("Side ray angle.")]
    [SerializeField, Range(0f, 90f)] float _whiskerAngle = 35f;

    [Header("Predictive")]

    [Tooltip("SphereCast radius.")]
    [SerializeField, Min(.05f)] float _bodyRadius = .4f;

    [Header("Debug")]

    [SerializeField] bool _showGizmos = true;

    [SerializeField] Color _rayColor = Color.green;
    [SerializeField] Color _hitColor = Color.red;
    [SerializeField] Color _avoidColor = Color.magenta;

    SteeringController _steeringController;

    Vector3 _lastAvoidance;

    bool _hasHit;

    float _currentLookAhead;

    RaycastHit _centerHit;
    RaycastHit _leftHit;
    RaycastHit _rightHit;
    RaycastHit _predictHit;

    void Awake()
    {
        _steeringController = GetComponent<SteeringController>();
    }

    void Update()
    {
        _hasHit = false;

        if (_automaticLookAhead)
        {
            float speedPercent =
                _steeringController.Velocity.magnitude / Mathf.Max(_steeringController.MaxSpeed, .01f);

            _currentLookAhead = Mathf.Lerp(.5f, _lookAhead, speedPercent);
        }
        else _currentLookAhead = _lookAhead;

        Vector3 avoidance =
            _mode == AvoidMode.Whiskers
            ? AvoidWithWhiskers()
            : AvoidWithPrediction();

        _lastAvoidance = avoidance;

        if (avoidance != Vector3.zero)
            _steeringController.AddSteering(avoidance, _weight);
    }

    bool CastWhisker(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        return Physics.Raycast(
            origin, direction, out hit,
            _currentLookAhead, _obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    Vector3 AvoidWithWhiskers()
    {
        Vector3 origin = transform.position + Vector3.up * GroundOffset;
        Vector3 forward = transform.forward;

        Vector3 result = Vector3.zero;

        if (CastWhisker(origin, forward, out _centerHit))
        {
            float urgency = 1f - (_centerHit.distance / _currentLookAhead);

            result += Vector3.Reflect(forward, _centerHit.normal) * urgency;

            _hasHit = true;
        }

        Vector3 left = Quaternion.Euler(0f, -_whiskerAngle, 0f) * forward;

        if (CastWhisker(origin, left, out _leftHit))
        {
            float urgency = 1f - (_leftHit.distance / _currentLookAhead);

            result += transform.right * urgency;

            _hasHit = true;
        }

        Vector3 right = Quaternion.Euler(0f, _whiskerAngle, 0f) * forward;

        if (CastWhisker(origin, right, out _rightHit))
        {
            float urgency = 1f - (_rightHit.distance / _currentLookAhead);

            result -= transform.right * urgency;

            _hasHit = true;
        }

        return result.normalized;
    }

    Vector3 AvoidWithPrediction()
    {
        Vector3 velocity = _steeringController.Velocity;

        if (velocity.sqrMagnitude < VelocityThreshold * VelocityThreshold)
            return Vector3.zero;

        Vector3 direction = velocity.normalized;
        Vector3 origin = transform.position + Vector3.up * _bodyRadius;

        if (Perception.HasLineOfSight_Sphere(
            origin, origin + direction * _currentLookAhead,
            _bodyRadius, _obstacleMask, out _predictHit))
        {
            return Vector3.zero;
        }

        _hasHit = true;

        Vector3 tangent =
            Vector3.Cross(_predictHit.normal, Vector3.up).normalized;

        float sign =
            Vector3.Dot(tangent, direction) >= 0f ? 1f : -1f;

        return (tangent * sign + _predictHit.normal).normalized;
    }

    #region Gizmos

    void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        float lookAhead =
            Application.isPlaying ? _currentLookAhead : _lookAhead;

        Vector3 origin = transform.position + Vector3.up * GroundOffset;

        if (_mode == AvoidMode.Whiskers)
        {
            DrawWhisker(origin, transform.forward, lookAhead);

            DrawWhisker(
                origin,
                Quaternion.Euler(0f, -_whiskerAngle, 0f) * transform.forward,
                lookAhead);

            DrawWhisker(
                origin,
                Quaternion.Euler(0f, _whiskerAngle, 0f) * transform.forward,
                lookAhead);

            DrawHit(_centerHit);
            DrawHit(_leftHit);
            DrawHit(_rightHit);
        }
        else
        {
            Vector3 dir =
                Application.isPlaying &&
                _steeringController != null &&
                _steeringController.Velocity.sqrMagnitude >
                VelocityThreshold * VelocityThreshold
                ? _steeringController.Velocity.normalized
                : transform.forward;

            Vector3 end = origin + dir * lookAhead;

            Gizmos.color = _rayColor;
            Gizmos.DrawLine(origin, end);

            Gizmos.DrawWireSphere(origin, _bodyRadius);
            Gizmos.DrawWireSphere(end, _bodyRadius);

            if (_hasHit)
            {
                Gizmos.color = _hitColor;
                Gizmos.DrawWireSphere(_predictHit.point, HitGizmoRadius);
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = _avoidColor;

            Gizmos.DrawLine(
                transform.position,
                transform.position +
                _lastAvoidance * AvoidVectorScale);
        }
    }

    void DrawWhisker(Vector3 origin, Vector3 direction, float distance)
    {
        Gizmos.color = _rayColor;
        Gizmos.DrawLine(origin, origin + direction * distance);
    }
    void DrawHit(RaycastHit hit)
    {
        if (hit.collider == null) return;

        Gizmos.color = _hitColor;
        Gizmos.DrawWireSphere(hit.point, HitGizmoRadius);
    }

    #endregion
}