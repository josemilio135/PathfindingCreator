using UnityEngine;

public class PersueState : BaseState<Hunter>
{
    AgentRunner _target;
    public PersueState(StateMachine fsm, Hunter controller, AgentRunner target) : base(fsm, controller)
    {
        _target = target;
    }

    public override void Update()
    {
        Debug.Log("Persui player");
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
    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
        controller.AlertTo(_target.transform.position);
    }

}
