using UnityEngine;
public class KinematicLocomotion : LocomotionBase
{
    public KinematicLocomotion(float rotationSpeed, float radius, float height, LayerMask obstacleMask)
        : base(rotationSpeed, radius, height, obstacleMask) { }

    public override void Move(Transform transform, Vector3 steering, float maxSpeed)
    {
        _velocity = Vector3.ClampMagnitude(_velocity + steering, maxSpeed);
        _velocity.y = 0f;

        ApplyVelocity(transform);
        Rotate(transform, _velocity);
    }

    public override void SetIdle(float brakeForce = 2f, bool instant = false)
    {
        _velocity = Vector3.zero;
    }
}