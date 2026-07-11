using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class EvadeSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField] Transform _pursuer;

    SteeringController _steeringController;
    SteeringController _pursuerController;

    void Awake()
    {
        _steeringController = GetComponent<SteeringController>();
        if (_pursuer != null) _pursuerController = _pursuer.GetComponent<SteeringController>();
    }

    void Update()
    {
        if (_pursuer == null) return;

        Vector3 steering = _pursuerController != null
            ? SteeringCalculator.Evade(transform.position, _steeringController.Velocity, _steeringController.MaxSpeed, _pursuerController)
            : SteeringCalculator.Evade(transform.position, _steeringController.Velocity, _steeringController.MaxSpeed, _pursuer.position, Vector3.zero);

        _steeringController.AddSteering(steering, _weight);
    }
}
