using UnityEngine;

public class ForceMovement
{
    float _maxSpeed;
    float _mass;
    float _maxForce;
    float _rotationSpeed;

    Vector3 _velocity;
    Vector3 _currentForce;

    public Vector3 Velocity => _velocity;

    public ForceMovement(float maxSpeed, float mass, float maxForce, float rotationSpeed)
    {
        _maxSpeed = maxSpeed;
        _mass = mass;
        _maxForce = maxForce;
        _rotationSpeed = rotationSpeed;
    }

    public void AddForce(Vector3 force) => _currentForce += force;

    public void ApplyMovement(Transform transform)
    {
        _currentForce = Vector3.ClampMagnitude(_currentForce, _maxForce);

        Vector3 acceleration = _currentForce / _mass;
        _velocity += acceleration * Time.deltaTime;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        _currentForce = Vector3.zero;
    }

    public void SetIdle(float brakeForce = 2f, bool instant = false)
    {
        if (instant)
        {
            _velocity = Vector3.zero;
            return;
        }

        AddForce(BrakeBehaviour.Calculate(_velocity, brakeForce));
        if (_velocity.sqrMagnitude < 0.0025f) _velocity = Vector3.zero;
    }
}