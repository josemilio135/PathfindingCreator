using UnityEngine;

public interface ILocomotion
{
    Vector3 Velocity { get; }
    void Move(Transform transform, Vector3 steering, float maxSpeed);
    void SetIdle(float brakeForce = 2f, bool instant = false);
}