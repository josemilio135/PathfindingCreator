using UnityEngine;
public abstract class LocomotionBase : ILocomotion
{
    protected readonly float _rotationSpeed;
    protected readonly float _radius;
    protected readonly float _height;
    protected readonly LayerMask _obstacleMask;
    protected Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    protected LocomotionBase(float rotationSpeed, float radius, float height, LayerMask obstacleMask)
    {
        _rotationSpeed = rotationSpeed;
        _radius = radius;
        _height = height;
        _obstacleMask = obstacleMask;
    }

    protected void Rotate(Transform transform, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Applies the velocity as movement, clamping it against obstacles first.
    /// Velocity is then synced to the actually applied delta 
    /// </summary>
    protected void ApplyVelocity(Transform transform)
    {
        Vector3 desiredDelta = _velocity * Time.deltaTime;

        Vector3 safeDelta = AgentPhysics.ClampMovement(
            transform.position, desiredDelta, _radius, _height, _obstacleMask);

        transform.position += safeDelta;

        if (Time.deltaTime > 0f)
            _velocity = safeDelta / Time.deltaTime;
    }

    public abstract void Move(Transform transform, Vector3 steering, float maxSpeed);
    public abstract void SetIdle(float brakeForce = 2f, bool instant = false);
}