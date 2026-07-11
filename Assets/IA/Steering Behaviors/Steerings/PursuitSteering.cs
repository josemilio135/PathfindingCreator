using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class PursuitSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField] Transform _prey;

    SteeringController _steeringController;
    SteeringController _preyController;

    void Awake()
    {
        _steeringController = GetComponent<SteeringController>();
        if (_prey != null) _preyController = _prey.GetComponent<SteeringController>();
    }

    void Update()
    {
        if (_prey == null) return;

        Vector3 steering = Vector3.zero;

        if (_preyController != null)
        {
            steering = SteeringCalculator.Pursuit(
                transform.position,
                _steeringController.Velocity,
                _steeringController.MaxSpeed,
                _preyController);
        }
        else
        {
            steering = SteeringCalculator.Pursuit(
                transform.position,
                _steeringController.Velocity,
                _steeringController.MaxSpeed,
                _prey.position,
                Vector3.zero);
        }

        _steeringController.AddSteering(steering, _weight);
    }
}
