using UnityEngine;
public class Hunter2 : Controller
{
    [Header("Patrol")]
    [SerializeField] Transform _waypointsContainer;
    [SerializeField] bool _pingPong = true;

    [Header("Target")]
    [SerializeField] Transform _target;
    
    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] bool _drawVision = true;
    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    AgentRunner _agent;
    Transform[] _waypoints;
    int _waypointIndex;
    int _waypointDir = 1;

    IdleState idleState;
    PersueState persueState;

    protected override void CreateStates() => idleState = new IdleState(stateMachine, this);
    protected override void SetInitialState() => stateMachine.SetState(idleState);
    /* 
     * 1. Idle -> esperar en cada waypoint   
     * 2. Patrullar al siguiente waypoint (cada uno tiene sus propios wps)
     * 3. Desde cualquiera si ve al jugador alerta (field of view) (con mini idle/animacion)
     * 4. Perseguir jugador
     * 5. Si fui alertado y llego y no está el jugador, me regreso
     * 6. Si le persigo y pierdo al jugador de vista, me regreso
    */
    protected override void SetTransitions()
    {
        Any(persueState, new FuncPredicate(() => CanSeeTarget()));
    }

    #region Predicates 

    bool CanSeeTarget()
    {
        if (_target == null) return false;

        Vector3 eyes = transform.position + _eyesOffset;
        Vector3 targetPos = _target.position + _eyesOffset;

        _isInRange = Perception.IsInRange(eyes, targetPos, _viewRange);
        if (!_isInRange) return false;

        _isInsideAngle = Perception.IsInViewAngle(eyes, transform.forward, targetPos, _fovAngle);
        if (!_isInsideAngle) return false;

        _canSeeTarget = Perception.HasLineOfSight(eyes, targetPos, _obstacleMask);
        if (!_canSeeTarget) return false;

        return true;
    }

    #endregion
}
public class Hunter : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform _waypointsContainer;
    [SerializeField] bool _pingPong = true;

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] bool _drawVision = true;
    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    AgentRunner _agent;
    Transform[] _waypoints;
    int _waypointIndex;
    int _waypointDir = 1;

    void Awake()
    {
        _agent = GetComponent<AgentRunner>();
        _agent.OnDestinationReached += NextWaypoint;

        if (_waypointsContainer != null)
        {
            _waypoints = new Transform[_waypointsContainer.childCount];
            for (int i = 0; i < _waypointsContainer.childCount; i++)
                _waypoints[i] = _waypointsContainer.GetChild(i);
        }
        else _waypoints = System.Array.Empty<Transform>();
    }

    void Start()
    {
        if (_waypoints.Length > 0) GoToCurrentWaypoint();
    }

    void Update()
    {
        // if (EvaluateVision(target)) ChaseTarget(target);
    }

    void GoToCurrentWaypoint()
    {
        if (_waypoints.Length == 0) return;
        _agent.SetDestination(_waypoints[_waypointIndex].position);
    }

    void NextWaypoint()
    {
        if (_pingPong)
        {
            _waypointIndex += _waypointDir;

            if (_waypointIndex >= _waypoints.Length)
            {
                _waypointDir = -1;
                _waypointIndex = _waypoints.Length - 2;
            }
            else if (_waypointIndex < 0)
            {
                _waypointDir = 1;
                _waypointIndex = 1;
            }
        }
        else _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;

        GoToCurrentWaypoint();
    }

    void ChaseTarget(Transform target)
    {
        if (target == null) return;
        _agent.SetDestination(target.position);
    }

    bool EvaluateVision(Transform target)
    {
        if (target == null) return false;

        Vector3 eyes = transform.position + _eyesOffset;
        Vector3 targetPos = target.position + _eyesOffset;

        _isInRange = Perception.IsInRange(eyes, targetPos, _viewRange);
        if (!_isInRange) return false;

        _isInsideAngle = Perception.IsInViewAngle(eyes, transform.forward, targetPos, _fovAngle);
        if (!_isInsideAngle) return false;

        _canSeeTarget = Perception.HasLineOfSight(eyes, targetPos, _obstacleMask);
        return _canSeeTarget;
    }

    #region Gizmos
    void OnDrawGizmos()
    {
        if (!_drawVision) return;

        Vector3 eyes = transform.position + _eyesOffset;

        Gizmos.color = _isInRange ? _rangeColor : Color.gray;
        Gizmos.DrawWireSphere(eyes, _viewRange);

        Gizmos.color = _isInsideAngle ? _viewAngleColor : Color.gray;
        Gizmos.DrawRay(eyes, Quaternion.Euler(0f, -_fovAngle * 0.5f, 0f) * transform.forward * _viewRange);
        Gizmos.DrawRay(eyes, Quaternion.Euler(0f, _fovAngle * 0.5f, 0f) * transform.forward * _viewRange);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(eyes, 0.1f);
    }
    #endregion
}
