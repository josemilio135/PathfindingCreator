public class ArriveToPointState : BaseState<Leader>
{
    public bool Arrived { get; private set; }

    public ArriveToPointState(StateMachine fsm, Leader controller) : base(fsm, controller) { }

    public override void OnEnter()
    {
        Arrived = false;
        controller.FollowDestination();
        controller.AgentPath.OnDestinationReached += HandleArrived;
        controller.SetStateText("Moving to point");
        controller.SetColorFOV("3B82F6");
    }
    public override void Update() { }
    public override void OnExit()
    {
        controller.AgentPath.OnDestinationReached -= HandleArrived;
    }

    void HandleArrived() => Arrived = true;
}