using UnityEngine;
public class FollowLeaderState : BaseState<NPCs>
{
    readonly Leader _leader;
    readonly float _stopDistance;
    float _originStopDistance;
    Vector3 _lastTarget;
    public FollowLeaderState(StateMachine fsm, NPCs controller, Leader leader, float stopDistance) : base(fsm, controller)
    {
        _leader = leader;
        _stopDistance = stopDistance;
    }
    public override void OnEnter()
    {
        _originStopDistance = controller.AgentPath.StopDistance;
        controller.AgentPath.StopDistance = _stopDistance;

        _lastTarget = Vector3.positiveInfinity;

        controller.SetStateText("Following leader");
        controller.SetColorFOV("4ADE80");
    }
    public override void Update()
    {
        Vector3 target = _leader.Position;

        bool leaderStopped = !_leader.AgentPath.IsMoving;

        if (leaderStopped)
        {
            if ((controller.Position - target).sqrMagnitude <= _stopDistance * _stopDistance)
            {
                controller.AgentPath.StopMovement();
                return;
            }
        }

        if ((target - _lastTarget).sqrMagnitude < .25f) return;

        controller.AgentPath.SetDestination(target);
        _lastTarget = target;
    }
    public override void OnExit()
    {
        controller.LastKnownPos = _leader.Position;
        controller.AgentPath.StopDistance = _originStopDistance;
    }
}