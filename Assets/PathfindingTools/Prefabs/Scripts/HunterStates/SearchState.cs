using UnityEngine;

public class SearchState : BaseState<Hunter>
{
    public SearchState(StateMachine fsm, Hunter controller) : base(fsm, controller)
    {
    }

    public override void OnEnter()
    {
        controller.ClearAlert();
        controller.AgentPath.SetDestination(controller.LastKnownPos);
    }

    public override void Update() { }

    public override void OnExit()
    {
    }
}
