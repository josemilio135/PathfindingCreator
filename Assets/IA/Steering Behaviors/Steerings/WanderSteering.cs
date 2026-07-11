using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class WanderSteering : MonoBehaviour
{
    [SerializeField, Range(0f, 3f)] float _weight = 1f;
    [SerializeField, Min(0f)] float _radius = 2f;
    [SerializeField, Min(0f)] float _distance = 4f;
    [SerializeField, Min(0f)] float _jitter = 40f;

    SteeringController _steeringController;
    Vector3 _wanderTarget;

    void Awake() => _steeringController = GetComponent<SteeringController>();

    void Update()
    {
        Vector3 steering = SteeringCalculator.Wander(
            transform.position, transform.forward, _steeringController.MaxSpeed,
            ref _wanderTarget, _radius, _distance, _jitter);

        _steeringController.AddSteering(steering, _weight);
    }
}
