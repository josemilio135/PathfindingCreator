using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class ArriveSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField] Transform _target;
    [SerializeField, Min(0f)] float _slowingRadius = 2f;
    [SerializeField] float _arrivalDistance = .1f;

    SteeringController _steeringController;

    void Awake() => _steeringController = GetComponent<SteeringController>();

    void Update()
    {
        if (_target == null) return;
        if (Vector3.Distance(transform.position, _target.position) <= _arrivalDistance)
            return;

        Vector3 steering = SteeringCalculator.Arrive(
            transform.position, _target.position,
            _steeringController.Velocity, _steeringController.MaxSpeed,
            _slowingRadius);

        _steeringController.AddSteering(steering, _weight);
    }
}
