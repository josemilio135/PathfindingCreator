using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class Hunter : Controller
{

    [Header("Patrol")]
    [SerializeField] Transform _waypointsContainer;
    [SerializeField] float _lookAroundTime = 2f;
    [SerializeField] float searchRotationAngle = 90f;
    [SerializeField] bool _patrolPingPong = true;

    [Header("Target")]
    [SerializeField] AgentRunner _target;

    [Header("Alert")]
    [SerializeField] List<Hunter> _allies = new();

    [Header("Vision")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);

    [Header("Debug")]
    [SerializeField] TMP_Text stateText;
    [SerializeField] bool _drawVision = true;
    [SerializeField] bool _drawRange = false;
    [SerializeField] Color _rangeColor = Color.cyan;
    [SerializeField] Color _viewAngleColor = Color.blue;

    public AgentRunner AgentPath { get; private set; }
    public bool PatrolPingPong => _patrolPingPong;
    public bool IsAlerted { get; set; }
    public bool IsPursue { get; set; }
    public Vector3 LastKnownPos { get; set; }

    bool _isInRange;
    bool _isInsideAngle;
    bool _canSeeTarget;


    LookAroundState lookAroundState;
    PatrolState patrolState;
    PersueState pursueState;
    GoToAlertState goToAlertState;

    protected override void SetInitialState() => stateMachine.SetState(lookAroundState);
    protected override void CreateStates()
    {
        AgentPath = GetComponent<AgentRunner>();

        lookAroundState = new LookAroundState(stateMachine, this, _lookAroundTime, searchRotationAngle);
        patrolState = new PatrolState(stateMachine, this, _waypointsContainer);
        goToAlertState = new GoToAlertState(stateMachine, this);
        pursueState = new PersueState(stateMachine, this, _target);

    }
    protected override void SetTransitions()
    {
        Any(pursueState, new FuncPredicate(CanPursueTarget));
        Any(goToAlertState, new FuncPredicate(() => ShouldSearch()));

        At(lookAroundState, patrolState, new FuncPredicate(() => lookAroundState.Finished));

        At(patrolState, lookAroundState, new FuncPredicate(() => patrolState.ArriveToNextPoint));
        At(goToAlertState, lookAroundState, new FuncPredicate(() => goToAlertState.ArriveToAlertPoint));


        At(pursueState, goToAlertState, new FuncPredicate(() => !CanSeeTarget() && IsPursue));
    }


    void AlertAllies(Vector3 position)
    {
        AlertTo(position);
        foreach (var ally in _allies)
        {
            if (ally.IsAlerted) continue;
            ally.AlertTo(position);
        }
    }
    public void AlertTo(Vector3 position)
    {
        LastKnownPos = position;
        IsAlerted = true;
    }
    public void SetStateText(string text) => stateText.text = text;


    #region Predicates 
    bool ShouldSearch() => IsAlerted && !IsPursue;
    bool CanPursueTarget()
    {
        if (_target == null) return false;
        if (IsPursue) return false;
        if (!CanSeeTarget()) return false;
        AlertAllies(_target.transform.position);

        return AgentPath.HasDirectLOS(_target.transform.position);
    }
    bool CanSeeTarget()
    {
        _isInRange = false;
        _isInsideAngle = false;
        _canSeeTarget = false;

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
        Vector3 eyes = transform.position + _eyesOffset;

        if (_drawRange)
        {
            Gizmos.color = _isInRange ? _rangeColor : Color.gray;
            Gizmos.DrawWireSphere(eyes, _viewRange);
        }

        if (_drawVision)
        {
            Gizmos.color = _isInsideAngle ? _viewAngleColor : Color.gray;
            Gizmos.DrawRay(eyes, Quaternion.Euler(0f, -_fovAngle * 0.5f, 0f) * transform.forward * _viewRange);
            Gizmos.DrawRay(eyes, Quaternion.Euler(0f, _fovAngle * 0.5f, 0f) * transform.forward * _viewRange);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(eyes, 0.1f);
        }
    }
    #endregion
}
