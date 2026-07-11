using UnityEngine;

public class SteeringController : MonoBehaviour
{
    public enum LocomotionType { Kinematic, Dynamic }

    const float IdleThreshold = .001f;

    [Header("Locomotion")]

    [SerializeField] LocomotionType _locomotionType = LocomotionType.Kinematic;

    [SerializeField, Min(0f)] float _maxSpeed = 5f;
    [SerializeField, Min(0f)] float _rotationSpeed = 10f;
    [SerializeField, Min(0f)] float _maxSteeringForce = 3f;

    [Header("Dynamic Only")]

    [SerializeField, Min(.01f)] float _mass = 1f;
    [SerializeField, Min(0f)] float _maxForce = 8f;

    ILocomotion _locomotion;

    Vector3 _accumulatedSteering;

    public Vector3 Position => transform.position;
    public Vector3 Velocity => _locomotion.Velocity;
    public float MaxSpeed => _maxSpeed;

    void Awake()
    {
        _locomotion = _locomotionType == LocomotionType.Dynamic
            ? new DynamicLocomotion(_maxForce, _mass, _rotationSpeed)
            : new KinematicLocomotion(_rotationSpeed);
    }

    public void AddSteering(Vector3 steering, float weight = 1f)
    {
        _accumulatedSteering += steering * weight;
    }

    void LateUpdate()
    {
        Vector3 steering =
            Vector3.ClampMagnitude(_accumulatedSteering, _maxSteeringForce);

        if (steering.sqrMagnitude <= IdleThreshold * IdleThreshold)
            _locomotion.SetIdle();
        else
            _locomotion.Move(transform, steering, _maxSpeed);

        _accumulatedSteering = Vector3.zero;
    }
}