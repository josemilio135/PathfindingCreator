using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [SerializeField] float _maxSpeed = 7f;
    [Min(0.01f)][SerializeField] float _mass = 1f;
    [Space]
    [SerializeField] float _maxSteeringForce = 7f;
    [SerializeField] float _rotationSpeed = 10f;

    ForceMovement _motor;

    public Vector3 Velocity => _motor.Velocity;
    public float MaxSpeed => _maxSpeed;

    void Awake()
    {
        _motor = new ForceMovement(_maxSpeed, _mass, _maxSteeringForce, _rotationSpeed);
    }
    public void ApplyMovement() => _motor.ApplyMovement(transform);

    public void Seek(Vector3 targetPos)
        => _motor.AddForce(SeekBehaviour.Calculate(transform.position, targetPos, _motor.Velocity, _maxSpeed));

    public void Flee(Vector3 threatPos)
        => _motor.AddForce(FleeBehaviour.Calculate(transform.position, threatPos, _motor.Velocity, _maxSpeed));

    public void Arrive(Vector3 targetPos, float slowingRadius = 2f)
        => _motor.AddForce(ArriveBehaviour.Calculate(transform.position, targetPos, _motor.Velocity, _maxSpeed, slowingRadius));

    public void Pursuit(SteeringAgent prey)
        => _motor.AddForce(PursuitBehaviour.Calculate(transform.position, _motor.Velocity, _maxSpeed, prey.transform.position, prey.Velocity));

    public void Pursuit(Vector3 preyPosition, Vector3 preyVelocity)
        => _motor.AddForce(PursuitBehaviour.Calculate(transform.position, _motor.Velocity, _maxSpeed, preyPosition, preyVelocity));

    public void Evade(SteeringAgent pursuer)
        => _motor.AddForce(EvadeBehaviour.Calculate(transform.position, _motor.Velocity, _maxSpeed, pursuer.transform.position, pursuer.Velocity));

    public void Evade(Vector3 pursuerPosition, Vector3 pursuerVelocity)
        => _motor.AddForce(EvadeBehaviour.Calculate(transform.position, _motor.Velocity, _maxSpeed, pursuerPosition, pursuerVelocity));

    public void Wander(ref Vector3 wanderTarget, float radius = 2f, float distance = 4f, float jitter = 40f)
        => _motor.AddForce(WanderBehaviour.Calculate(transform.position, transform.forward, _maxSpeed, ref wanderTarget, radius, distance, jitter));

    public void SetIdle(float brakeForce = 2f, bool instant = false)
        => _motor.SetIdle(brakeForce, instant);

}