using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class SeekSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField] Transform _target;

    SteeringController _steeringController;

    void Awake() => _steeringController = GetComponent<SteeringController>();

    void Update()
    {
        if (_target == null) return;

        Vector3 steering = SteeringCalculator.Seek(
            transform.position, _target.position,
            _steeringController.Velocity, _steeringController.MaxSpeed);

        _steeringController.AddSteering(steering, _weight);
    }
}
