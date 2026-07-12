using UnityEngine;

public class ObstacleAvoidanceSteering : Steerings
{
    public enum AvoidMode { Predictive, Whiskers }

    const float VelocityThreshold = .01f;
    const float GroundOffset = .05f;
    const float AvoidVectorScale = .35f;

    [Header("General")]

    [Tooltip("Obstacle avoidance method.")]
    [SerializeField] AvoidMode _mode = AvoidMode.Predictive;

    [Tooltip("Layers treated as obstacles.")]
    [SerializeField] LayerMask _obstacleMask;

    [Space]

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

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] bool _showGizmos = true;
    [SerializeField] Color _gizmoColor = Color.cyan;
#endif

    Vector3 _lastAvoidance;
    float _currentLookAhead;

#if UNITY_EDITOR
    bool _hasHit;
    RaycastHit _predictHit;
#endif

    protected override Vector3 CalculateSteering()
    {
        if (_automaticLookAhead)
        {
            float speedPercent = Controller.Velocity.magnitude / Mathf.Max(Controller.MaxSpeed, .01f);
            _currentLookAhead = Mathf.Lerp(.5f, _lookAhead, speedPercent);
        }
        else _currentLookAhead = _lookAhead;

        Vector3 avoidance = _mode == AvoidMode.Whiskers ?
            AvoidWithWhiskers() : AvoidWithPrediction();

#if UNITY_EDITOR
        _lastAvoidance = avoidance;
#endif
        return avoidance;
    }

    #region Whiskers
    Vector3 AvoidWithWhiskers()
    {
        Vector3 origin = transform.position + Vector3.up * GroundOffset;
        Vector3 forward = transform.forward;

        Vector3 result = Vector3.zero;

        if (CastWhisker(origin, forward, out RaycastHit centerHit))
        {
            float urgency = 1f - (centerHit.distance / _currentLookAhead);
            result += Vector3.Reflect(forward, centerHit.normal) * urgency;
        }

        Vector3 left = Quaternion.Euler(0f, -_whiskerAngle, 0f) * forward;

        if (CastWhisker(origin, left, out RaycastHit leftHit))
        {
            float urgency = 1f - (leftHit.distance / _currentLookAhead);
            result += transform.right * urgency;
        }

        Vector3 right = Quaternion.Euler(0f, _whiskerAngle, 0f) * forward;

        if (CastWhisker(origin, right, out RaycastHit rightHit))
        {
            float urgency = 1f - (rightHit.distance / _currentLookAhead);
            result -= transform.right * urgency;
        }

        return result.normalized;
    }

    bool CastWhisker(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        return Physics.Raycast(
            origin, direction, out hit,
            _currentLookAhead, _obstacleMask,
            QueryTriggerInteraction.Ignore);
    }
    #endregion

    #region Prediction
    Vector3 AvoidWithPrediction()
    {
        Vector3 velocity = Controller.Velocity;

        if (velocity.sqrMagnitude < VelocityThreshold * VelocityThreshold)
        {
#if UNITY_EDITOR
            _hasHit = false;
#endif
            return Vector3.zero;
        }

        Vector3 direction = velocity.normalized;
        Vector3 origin = transform.position + Vector3.up * _bodyRadius;

        bool clear = Perception.HasLineOfSight_Sphere(
            origin, origin + direction * _currentLookAhead,
            _bodyRadius, _obstacleMask, out RaycastHit predictHit);

#if UNITY_EDITOR
        _hasHit = !clear;
        _predictHit = predictHit;
#endif

        if (clear) return Vector3.zero;

        Vector3 tangent = Vector3.Cross(predictHit.normal, Vector3.up).normalized;
        float sign = Vector3.Dot(tangent, direction) >= 0f ? 1f : -1f;

        return (tangent * sign + predictHit.normal).normalized;
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        Gizmos.color = _gizmoColor;

        float lookAhead = Application.isPlaying ? _currentLookAhead : _lookAhead;
        Vector3 origin = transform.position + Vector3.up * GroundOffset;

        if (_mode == AvoidMode.Whiskers)
        {
            Gizmos.DrawLine(origin, origin + transform.forward * lookAhead);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, -_whiskerAngle, 0f) * transform.forward * lookAhead);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, _whiskerAngle, 0f) * transform.forward * lookAhead);
        }
        else
        {
            Vector3 dir = Application.isPlaying && Controller != null &&
                Controller.Velocity.sqrMagnitude > VelocityThreshold * VelocityThreshold ?
                Controller.Velocity.normalized : transform.forward;

            Vector3 predictOrigin = transform.position + Vector3.up * _bodyRadius;
            float rayLength = _hasHit ? _predictHit.distance : lookAhead;

            Gizmos.DrawLine(predictOrigin, predictOrigin + dir * rayLength);

            Gizmos.DrawWireSphere(origin + transform.forward * lookAhead, _bodyRadius);
        }

        if (Application.isPlaying)
            Gizmos.DrawLine(transform.position, transform.position + _lastAvoidance * AvoidVectorScale);
    }
#endif
    #endregion
}