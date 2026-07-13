using UnityEngine;

public class Leader : Controller, IHaveTeamate
{
    [SerializeField] Teamates _team;
    [SerializeField] LayerMask _agentMask;

    [Header("Vision")]
    [SerializeField] FOV_Mesh _fovMesh;
    [SerializeField, Min(0)] float _viewRange = 10f;
    [SerializeField, Range(0f, 360f)] float _fovAngle = 90f;
    [SerializeField] Vector3 _eyesOffset = new(0f, 1.5f, 0f);
    [SerializeField] LayerMask _obstacleMask;

    /* --- STATES --- */
    IdleLeaderState idleLeaderState;
    AttackLeaderState attackLeaderState;


    /* --- PUBLICS --- */
    public Teamates Team => _team;
    public Vector3 Position => transform.position;
    public Vector3 LastKnownPos { get; set; }
    public AgentRunner AgentPath { get; private set; }


    /* --- PRIVATES --- */
    IHaveTeamate currentEnemyTarget;
    Vector3 eyes => transform.position + _eyesOffset;
    readonly Collider[] _hits = new Collider[32];


    void Initialice()
    {
        AgentPath = GetComponent<AgentRunner>();
        if (_fovMesh) _fovMesh.SetConfig(_viewRange, _fovAngle, _obstacleMask, _eyesOffset);

    }
    protected override void SetInitialState() => stateMachine.SetState(idleLeaderState);

    protected override void CreateStates()
    {
        Initialice();
        idleLeaderState = new IdleLeaderState(stateMachine, this);
        attackLeaderState = new AttackLeaderState(stateMachine, this);
    }

    protected override void SetTransitions()
    {
        Any(attackLeaderState, new FuncPredicate(FindEnemy));

        // At(lookAroundState, patrolState, new FuncPredicate(() => lookAroundState.Finished)); 
    }
    bool FindEnemy()
    {
        if (currentEnemyTarget != null)
        {
            if (CanSeeTarget(currentEnemyTarget))
                return true;

            currentEnemyTarget = null;
        }

        int count = Physics.OverlapSphereNonAlloc(
            eyes, _viewRange, _hits, _agentMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];

            if (!hit.TryGetComponentInParent<IHaveTeamate>(out var team))
                continue;

            if (team.Team == Team)
                continue;

            if (!CanSeeTarget(team))
                continue;

            currentEnemyTarget = team;
            return true;
        }

        return false;
    }
    bool CanSeeTarget(IHaveTeamate target)
    {
        Vector3 targetPos = target.Position + _eyesOffset;

        if (!Perception.IsInRange(eyes, targetPos, _viewRange))
            return false;

        if (!Perception.IsInViewAngle(eyes, transform.forward, targetPos, _fovAngle))
            return false;

        if (!Perception.HasLineOfSight(eyes, targetPos, _obstacleMask))
            return false;

        Debug.Log("Enemy");
        return true;
    }

    // public void SetStateText(string text) => stateText.text = text;
    public void SetColorFOV(string hexadecimal, float alpha = .2f)
    {
        if (_fovMesh == null) return;
        ColorUtility.TryParseHtmlString("#" + hexadecimal, out Color color);
        color.a = alpha;

        _fovMesh.SetColor(color);
    }
    void OnValidate()
    {
        if (_fovMesh == null) return;

        _fovMesh.SetConfig(
            _viewRange,
            _fovAngle,
            _obstacleMask,
            _eyesOffset);
    }
}
