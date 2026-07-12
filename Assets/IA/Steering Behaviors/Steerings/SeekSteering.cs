using UnityEngine;

public class SeekSteering : Steerings
{
    [SerializeField] Transform _target;

    protected override Vector3 CalculateSteering()
    {
        if (_target == null) return Vector3.zero;

        return SteeringCalculator.Seek(
            transform.position,
            _target.position,
            Controller.Velocity,
            Controller.MaxSpeed);
    }

}
