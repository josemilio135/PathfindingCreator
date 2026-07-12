using UnityEngine;
 
public class PursuitSteering : Steerings
{ 
    [SerializeField] Transform _prey;
     
    SteeringController _preyController;

    protected override void Awake()
    {
        base.Awake();
        if (_prey != null) _preyController = _prey.GetComponent<SteeringController>();
    }
     
    protected override Vector3 CalculateSteering()
    {
        Vector3 steering = Vector3.zero;

        if (_prey == null) return steering;

        if (_preyController != null)
        {
            steering = SteeringCalculator.Pursuit(
                transform.position,
                Controller.Velocity,
                Controller.MaxSpeed,
                _preyController);
        }
        else
        {
            steering = SteeringCalculator.Pursuit(
                transform.position,
                Controller.Velocity,
                Controller.MaxSpeed,
                _prey.position,
                Vector3.zero);
        }

        return steering; 
    }
}
