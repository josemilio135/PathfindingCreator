using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public abstract class Steerings : MonoBehaviour
{
    [Tooltip("Importance compared to other steering.")]
    [SerializeField, Range(0f, 10f)] protected float _weight = 1f; 
    protected SteeringController Controller { get; private set; }

    protected virtual void Awake()
    {
        Controller = GetComponent<SteeringController>();
    }

    protected virtual void Update()
    {
        Vector3 steering = CalculateSteering();

        if (steering != Vector3.zero) 
            Controller.AddSteering(steering, _weight);
    }

    protected abstract Vector3 CalculateSteering();
}