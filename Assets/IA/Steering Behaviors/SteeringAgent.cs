using UnityEngine;

[DisallowMultipleComponent]
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] protected float _maxSpeed = 7f;
    [Min(0.01f)][SerializeField] protected float _mass = 1f;
    [Space]
    [SerializeField] protected float _maxSteeringForce = 7f;
    [SerializeField] float _rotationSpeed = 10f;

    Vector3 _velocity;
    Vector3 _steeringForce;
    public Vector3 Velocity => _velocity;
    public float MaxSpeed => _maxSpeed;

    public void AddForce(Vector3 force) => _steeringForce += force;

    public void ApplyMovement()
    {
        _steeringForce = Vector3.ClampMagnitude(_steeringForce, _maxSteeringForce);

        Vector3 acceleration = _steeringForce / _mass;

        _velocity += acceleration * Time.deltaTime;

        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        _steeringForce = Vector3.zero;
    }

    public void DoIdle(float brakeForce = 2f, bool instant = false)
    {
        if (instant)
        {
            _velocity = Vector3.zero;
            return;
        }
        AddForce(SteeringBehaviours.Brake(_velocity, brakeForce));
        if (_velocity.sqrMagnitude < 0.0025f) _velocity = Vector3.zero;
    }
}
