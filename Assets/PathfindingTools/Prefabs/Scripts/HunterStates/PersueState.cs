using UnityEngine;

public class PersueState : BaseState<Hunter>
{
    AgentRunner _target;
    float _stopDistance, _originStopDistance;
    public PersueState(StateMachine fsm, Hunter controller, AgentRunner target, float stopDistance) : base(fsm, controller)
    {
        _target = target;
        _stopDistance = stopDistance;

        _originStopDistance = controller.AgentPath.StopDistance;
    }

    public override void OnEnter()
    {
        controller.IsPursue = true;
        controller.AgentPath.StopDistance = _stopDistance;

        Debug.Log("Persui player");
        controller.SetStateText("!");
        controller.SetColorFOV("FF3B00");
    }

    public override void Update()
    {
        controller.AgentPath.SetDestination(_target.transform.position);


        //  var pursuitForce = PursuitBehaviour.Calculate(
        //        controller.transform.position, controller.AgentPath.Velocity,
        //        controller.AgentPath.MoveSpeed,
        //        _target.transform.position, _target.Velocity);
        //
        //  Vector3 predictedPos = controller.transform.position + pursuitForce;
        //
        //  controller.AgentPath.SetDestination(predictedPos);

    }

    public override void OnExit()
    {
        controller.IsPursue = false;
        controller.LastKnownPos = _target.transform.position;

        controller.AgentPath.StopDistance = _originStopDistance;
    }
}
