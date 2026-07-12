using UnityEngine;
 
public class EvadeSteering : Steerings
{ 
    [SerializeField] Transform _pursuer;
     
    SteeringController _pursuerController;

    protected override void Awake()
    {
        base.Awake();
        if (_pursuer != null) _pursuerController = _pursuer.GetComponent<SteeringController>();
    }

    protected override Vector3 CalculateSteering()
    { 
        if (_pursuer == null) return Vector3.zero;

        return _pursuerController != null  ? 
          SteeringCalculator.Evade(transform.position,  Controller.Velocity,  Controller.MaxSpeed, _pursuerController) :
          SteeringCalculator.Evade(transform.position,  Controller.Velocity,  Controller.MaxSpeed, _pursuer.position, Vector3.zero);
         
    }
}
