using UnityEngine;
public class Hunter : Controller
{

    [Header("Patrol")]
    [SerializeField] Transform _waypointsContainer;
    [SerializeField] float _lookAroundTime = 2f;
    [SerializeField] bool _patrolPingPong = true;

    [Header("Target")]
    [SerializeField] AgentRunner _target;

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] bool _drawVision = true;
    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;

    public bool PatrolPingPong => _patrolPingPong;
    public AgentRunner AgentPath { get; set; }

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;

    LookAroundState lookAroundState;
    PatrolState patrolState;
    PersueState persueState;
    SearchState searchState;

    protected override void SetInitialState() => stateMachine.SetState(lookAroundState);
    protected override void CreateStates()
    {
        AgentPath = GetComponent<AgentRunner>();

        lookAroundState = new LookAroundState(stateMachine, this, _lookAroundTime);
        patrolState = new PatrolState(stateMachine, this, _waypointsContainer);
        persueState = new PersueState(stateMachine, this, _target);
        searchState = new SearchState(stateMachine, this);

    }

    /* 
     * 1. LookAround -> esperar en cada waypoint   //rota hacia ambos lados viendo en 360 en 2 tandas
     * 5. Si fui alertado y llego y no está el jugador, me regreso
    */
    protected override void SetTransitions()
    {
        Any(persueState, new FuncPredicate(() => CanPursuiTarget()));
        Any(searchState, new FuncPredicate(() => MustSearchTarget()));

        At(lookAroundState, patrolState, new FuncPredicate(() => !IsLookAround));
        At(persueState, lookAroundState, new FuncPredicate(() => !CanSeeTarget()));
        At(searchState, lookAroundState, new FuncPredicate(() => ArriveAlertPos()));

    }

    public bool IsLookAround { get; set; }
    public bool IsAlert { get; set; } = false;
    public Vector3 LastKnownPos { get; private set; }

    public void AlertTo(Vector3 position)
    {
        LastKnownPos = position;
        IsAlert = true;
    }
    public void ClearAlert() => IsAlert = false;


    #region Predicates 
    bool MustSearchTarget() => IsAlert || (CanSeeTarget() && !CanPursuiTarget());
    bool CanPursuiTarget() => CanSeeTarget() && AgentPath.HasDirectLOS(_target.transform.position);
    bool ArriveAlertPos()
    {
        return true;
    }
    bool CanSeeTarget()
    {
        if (_target == null) return false;

        Vector3 eyes = transform.position + _eyesOffset;
        Vector3 targetPos = _target.gameObject.transform.position + _eyesOffset;

        _isInRange = Perception.IsInRange(eyes, targetPos, _viewRange);
        if (!_isInRange) return false;

        _isInsideAngle = Perception.IsInViewAngle(eyes, transform.forward, targetPos, _fovAngle);
        if (!_isInsideAngle) return false;

        _canSeeTarget = Perception.HasLineOfSight(eyes, targetPos, _obstacleMask);
        if (!_canSeeTarget) return false;

        return true;
    }
    #endregion

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
