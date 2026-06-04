using UnityEngine;

[RequireComponent(typeof(SteeringAgent))]
public class Walker : MonoBehaviour
{
    [SerializeField] Transform _target;

    [Header("Weights")]
    [SerializeField, Range(0f, 3f)] float _weightArrive = 1f;
    [SerializeField] float _slowingRadius = 4f;

    SteeringAgent _agent;

    void Awake() => _agent = GetComponent<SteeringAgent>();

    void Update()
    {
        if (_target != null)
        {
            _agent.AddForce(SteeringBehaviours.Arrive(
                transform.position, _target.position,
                _agent.Velocity, _agent.MaxSpeed,
                _slowingRadius) * _weightArrive);
        }

        else _agent.DoIdle();

        _agent.ApplyMovement(); 
    }
}