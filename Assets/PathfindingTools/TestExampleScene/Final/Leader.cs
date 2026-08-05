using UnityEngine;
public class Leader : EntityController
{
    [Header("Search")]
    [SerializeField] float _lookAroundTime = 3f;
    [SerializeField] float _searchRotationAngle = 60f;
    public bool HasDestinationRequest { get; private set; }

    //  *-- States -- *
    IdleLeaderState idleLeaderState;
    ArriveToPointState arriveToPointState;
    AttackState attackState;
    FleeState fleeState;
    RetreatState retreatState;
    DeadState deadState;

    Vector3 _clickDestination;

    protected override void SetInitialState() => stateMachine.SetState(idleLeaderState);
    protected override void CreateStates()
    {
        Initialice();
        idleLeaderState = new IdleLeaderState(stateMachine, this, _lookAroundTime, _searchRotationAngle);
        arriveToPointState = new ArriveToPointState(stateMachine, this);
        attackState = new AttackState(stateMachine, this);
        fleeState = new FleeState(stateMachine, this);
        retreatState = new RetreatState(stateMachine, this);
        deadState = new DeadState(stateMachine, this);
    }
    protected override void SetTransitions()
    {
        Any(deadState, new FuncPredicate(() => Health.IsDead));
        Any(arriveToPointState, new FuncPredicate(() => HasDestinationRequest && !Health.IsDead));
        Any(fleeState, new FuncPredicate(() => Health.IsWeak && !Health.IsDead && !HasDestinationRequest));
        Any(attackState, new FuncPredicate(() => !Health.IsWeak && !Health.IsDead && !HasDestinationRequest && FindEnemy()));

        At(attackState, retreatState, new FuncPredicate(() => CurrentEnemyTarget == null));
        At(retreatState, idleLeaderState, new FuncPredicate(() => !AgentPath.IsMoving));

        At(arriveToPointState, idleLeaderState, new FuncPredicate(() => arriveToPointState.Arrived));
        At(deadState, idleLeaderState, new FuncPredicate(() => !Health.IsDead));
        At(fleeState, idleLeaderState, new FuncPredicate(() => !Health.IsWeak));
    }
    public void RequestDestination(Vector3 point)
    {
        _clickDestination = point;
        HasDestinationRequest = true;
    }
    public void FollowDestination()
    {
        AgentPath.SetDestination(_clickDestination);
        HasDestinationRequest = false;
    }
}