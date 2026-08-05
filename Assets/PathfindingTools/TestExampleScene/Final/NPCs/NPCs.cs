using UnityEngine;
public class NPCs : EntityController
{
    [Header("Leader")]
    [SerializeField] Leader _leader;
    [SerializeField] float _followStopDistance = 2f;
    FollowLeaderState followLeaderState;
    AttackState attackState;
    FleeState fleeState;
    RetreatState retreatState;
    DeadState deadState;
    protected override void SetInitialState() => stateMachine.SetState(followLeaderState);
    protected override void CreateStates()
    {
        Initialice();
        followLeaderState = new FollowLeaderState(stateMachine, this, _leader, _followStopDistance);
        attackState = new AttackState(stateMachine, this);
        fleeState = new FleeState(stateMachine, this);
        retreatState = new RetreatState(stateMachine, this);
        deadState = new DeadState(stateMachine, this);
    }
    protected override void SetTransitions()
    {
        Any(deadState, new FuncPredicate(() => Health.IsDead));
        Any(fleeState, new FuncPredicate(() => Health.IsWeak && !Health.IsDead));
        Any(attackState, new FuncPredicate(() => !Health.IsWeak && !Health.IsDead && FindEnemy()));

        At(attackState, retreatState, new FuncPredicate(() => CurrentEnemyTarget == null));
        At(retreatState, followLeaderState, new FuncPredicate(() => !AgentPath.IsMoving));

        At(deadState, followLeaderState, new FuncPredicate(() => !Health.IsDead));
        At(fleeState, followLeaderState, new FuncPredicate(() => !Health.IsWeak));
    }
}