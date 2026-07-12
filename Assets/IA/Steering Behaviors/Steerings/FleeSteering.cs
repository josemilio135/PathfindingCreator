using UnityEngine;

public class FleeSteering : Steerings
{
    [SerializeField] Transform _threat;

    protected override Vector3 CalculateSteering()
    {
        if (_threat == null) return Vector3.zero;

        return SteeringCalculator.Flee(
            transform.position,
            _threat.position,
             Controller.Velocity,
             Controller.MaxSpeed);
    }
}
