using UnityEngine;

public class SteeringController : MonoBehaviour
{
    public enum LocomotionType { Kinematic, Dynamic }

    [Header("Agent Collision")]

    [Tooltip("Agent body radius used to block movement against obstacles.")]
    [SerializeField, Min(.01f)] float _radius = .5f;
    [Tooltip("Agent body height used to block movement against obstacles.")]
    [SerializeField, Min(.1f)] float _height = 2f;
    [Tooltip("Layers this agent cannot move through. Independent from any pathfinding obstacle mask.")]
    [SerializeField] LayerMask _obstacleMask;

    [Header("Locomotion")]

    [Tooltip("Kinematic: direct movement by direction, no mass or inertia.\nDynamic: uses forces, mass and acceleration.")]
    [SerializeField] LocomotionType _locomotionType = LocomotionType.Kinematic;
    [Tooltip("Maximum movement speed.")]
    [SerializeField, Min(0f)] float _maxSpeed = 5f;
    [Tooltip("Rotation speed towards the movement direction.")]
    [SerializeField, Min(0f)] float _rotationSpeed = 10f;

    [Header("Dynamic Only")]

    [Tooltip("Agent mass. Higher mass means less acceleration for the same force.")]
    [SerializeField, Min(.01f)] float _mass = 1f;
    [Tooltip("Max force the dynamic motor can apply per frame.")]
    [SerializeField, Min(0f)] float _maxForce = 8f;

    [Header("Debug")]
    [SerializeField] bool _showSpeed;
    [SerializeField] bool _logSpeedToConsole;

    ILocomotion _locomotion;
    Vector3 _accumulatedSteering;
    float _totalWeight;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => _locomotion.Velocity;
    public float MaxSpeed => _maxSpeed;
    public float CurrentSpeed => _locomotion.Velocity.magnitude;

    void Awake()
    {
        _locomotion = _locomotionType == LocomotionType.Dynamic ?
            new DynamicLocomotion(_maxForce, _mass, _rotationSpeed, _radius, _height, _obstacleMask) :
            new KinematicLocomotion(_rotationSpeed, _radius, _height, _obstacleMask);
    }
    public void AddSteering(Vector3 steering, float weight = 1f)
    {
        if (weight <= 0f) return;

        _accumulatedSteering += steering * weight;
        _totalWeight += weight;
    }
    void LateUpdate()
    {
        bool hasSteering = _totalWeight > 0f;
        Vector3 steering = hasSteering ? _accumulatedSteering / _totalWeight : Vector3.zero;

        if (hasSteering) _locomotion.Move(transform, steering, _maxSpeed);
        else _locomotion.SetIdle();

        if (_logSpeedToConsole)
            Debug.Log($"{gameObject.name}: speed = {CurrentSpeed:F2}/{_maxSpeed:F2}");

        _accumulatedSteering = Vector3.zero;
        _totalWeight = 0f;
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!_showSpeed || !Application.isPlaying)
            return;
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.2f,
            $"Speed: {CurrentSpeed:F2}/{MaxSpeed:F2}");
    }
#endif
}