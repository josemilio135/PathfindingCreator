using UnityEngine;
public class DynamicLocomotion : LocomotionBase
{
    readonly float _mass;
    readonly float _maxForce;

    public DynamicLocomotion(float maxForce, float mass, float rotationSpeed, float radius, float height, LayerMask obstacleMask)
        : base(rotationSpeed, radius, height, obstacleMask)
    {
        _maxForce = maxForce;
        _mass = mass;
    }

    public override void Move(Transform transform, Vector3 steering, float maxSpeed)
    {
        Vector3 force = Vector3.ClampMagnitude(steering, _maxForce);
        Vector3 acceleration = force / _mass;
        _velocity = Vector3.ClampMagnitude(_velocity + acceleration * Time.deltaTime, maxSpeed);
        _velocity.y = 0f;

        ApplyVelocity(transform);
        Rotate(transform, _velocity);
    }

    public override void SetIdle(float brakeForce = 2f, bool instant = false)
    {
        if (instant)
        {
            _velocity = Vector3.zero;
            return;
        }

        _velocity += SteeringCalculator.Brake(_velocity, brakeForce) * Time.deltaTime;

        if (_velocity.sqrMagnitude < 0.0025f) _velocity = Vector3.zero;
    }
}