public class GoToAlertState : BaseState<Hunter>
{
    public GoToAlertState(StateMachine fsm, Hunter controller) : base(fsm, controller)
    {
    }
    public bool ArriveToAlertPoint { get; private set; } = false;
    public override void OnEnter()
    {
        ArriveToAlertPoint = false;
        controller.AgentPath.OnDestinationReached += ArriveToPoint;

        controller.AgentPath.SetDestination(controller.LastKnownPos);

        controller.SetStateText("Alert!");
    }

    public override void Update() { }

    public override void OnExit()
    {
        controller.AgentPath.OnDestinationReached -= ArriveToPoint;

        controller.IsAlerted = false;
    }

    void ArriveToPoint() => ArriveToAlertPoint = true;

}
