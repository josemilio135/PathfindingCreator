using UnityEngine;
public class WanderSteering : Steerings
{
    [SerializeField, Min(0f)] float _radius = 2f;
    [SerializeField, Min(0f)] float _distance = 4f;
    [SerializeField, Range(0, 360)] float _jitter = 40f;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] bool _showGizmos = true;
#endif

    Vector3 _wanderTarget;

    protected override Vector3 CalculateSteering()
    {
        return SteeringCalculator.Wander(
           transform.position,
           transform.forward,
           Controller.Velocity,
           Controller.MaxSpeed,
           ref _wanderTarget,
           _radius,
           _distance,
           _jitter);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!_showGizmos || !Application.isPlaying) return;

        Vector3 circleCenter = transform.position + transform.forward * _distance;
        Vector3 targetPoint = circleCenter + _wanderTarget;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(circleCenter, _radius);
        Gizmos.DrawSphere(targetPoint, 0.15f);
        Gizmos.DrawLine(transform.position, targetPoint);
    }
#endif
}