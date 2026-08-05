using UnityEngine;

[RequireComponent(typeof(SteeringController))]
public class FlockingSteering : Steerings, IFlockMember
{
    [SerializeField] FlockManager _manager;

    [Header("Radius")]
    [SerializeField, Min(.01f)] float _separationRadius = 2f;

    [Header("Weights")]
    [SerializeField, Range(0f, 3f)] float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] float _alignmentWeight = 1f;
    [SerializeField, Range(0f, 3f)] float _cohesionWeight = 1f;

    [Header("Neighbors")]
    [SerializeField, Min(1)] int _maxNeighbors = 16;
    [SerializeField, Min(.01f)] float _neighborRadius = 5f;

    IFlockMember[] _neighborBuffer;

    public Vector3 Position => transform.position;
    public Vector3 Velocity => Controller.Velocity;

    protected override void Awake()
    {
        base.Awake();
        _neighborBuffer = new IFlockMember[_maxNeighbors];
    }
    private void Start()
    {
        _manager ??= FlockManager.Instance;
    }
    public void SetManager(FlockManager manager) => _manager = manager;

    void OnEnable() => _manager?.Register(this);
    void OnDisable() => _manager?.Unregister(this);

    protected override Vector3 CalculateSteering()
    {
        if (_manager == null) return Vector3.zero;

        float queryRadius = Mathf.Max(_separationRadius, _neighborRadius);
        int count = _manager.GetNeighbors(this, queryRadius, _neighborBuffer);
        if (count == 0) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        Vector3 avgVelocity = Vector3.zero;
        Vector3 avgPosition = Vector3.zero;

        int cohesionCount = 0;

        for (int i = 0; i < count; i++)
        {
            IFlockMember other = _neighborBuffer[i];

            Vector3 offset = transform.position - other.Position;
            float distance = offset.magnitude;

            if (distance < 0.0001f) continue;

            if (distance <= _separationRadius)
            {
                separation += offset / (distance * distance);
            }

            if (distance <= _neighborRadius)
            {
                avgVelocity += other.Velocity;
                avgPosition += other.Position;

                cohesionCount++;
            }
        }

        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        if (cohesionCount > 0)
        {
            avgVelocity /= cohesionCount;
            alignment = avgVelocity - Velocity;

            avgPosition /= cohesionCount;
            cohesion = avgPosition - transform.position;
        }

        return separation.normalized * _separationWeight
             + alignment.normalized * _alignmentWeight
             + cohesion.normalized * _cohesionWeight;
    }
}