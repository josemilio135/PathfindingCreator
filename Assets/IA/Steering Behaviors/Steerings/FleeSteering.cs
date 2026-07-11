using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class FleeSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField] Transform _threat;

    SteeringController _steeringController;

    void Awake() => _steeringController = GetComponent<SteeringController>();

    void Update()
    {
        if (_threat == null) return;

        Vector3 steering = SteeringCalculator.Flee(
            transform.position, _threat.position,
            _steeringController.Velocity, _steeringController.MaxSpeed);

        _steeringController.AddSteering(steering, _weight);
    }
}
