public class SearchState : BaseState<Hunter>
{
    public SearchState(StateMachine fsm, Hunter controller) : base(fsm, controller)
    {
    }

    public override void OnEnter()
    {
        controller.AgentPath.SetDestination(controller.LastKnownPos);

        controller.SetStateText("?");
    }

    public override void Update() { }

    public override void OnExit()
    {
        controller.IsAlerted = false;
    }
}
