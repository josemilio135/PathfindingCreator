using UnityEngine;

public class ArriveSteering : Steerings
{
    [SerializeField] Transform _target;
    [SerializeField, Min(0f)] float _slowingRadius = 2f;
    [SerializeField] float _arrivalDistance = .1f;

    protected override Vector3 CalculateSteering()
    {
        if (_target == null) return Vector3.zero;

        if (Vector3.Distance(transform.position, _target.position) <= _arrivalDistance) return Vector3.zero;

        return SteeringCalculator.Arrive(
             transform.position,
             _target.position,
             Controller.Velocity,
             Controller.MaxSpeed,
             _slowingRadius);

    }
}
