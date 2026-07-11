using UnityEngine;

public abstract class LocomotionBase : ILocomotion
{
    protected readonly float _rotationSpeed;
    protected Vector3 _velocity;

    public Vector3 Velocity => _velocity;

    protected LocomotionBase(float rotationSpeed)
    {
        _rotationSpeed = rotationSpeed;
    }

    protected void Rotate(Transform transform, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    public abstract void Move(Transform transform, Vector3 steering, float maxSpeed);
    public abstract void SetIdle(float brakeForce = 2f, bool instant = false);
}