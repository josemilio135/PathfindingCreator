using UnityEngine;

public class LookAroundState : BaseState<Hunter>
{
    public LookAroundState(StateMachine fsm, Hunter controller, float lookAroundTime) : base(fsm, controller)
    {
        _lookAroundTime = lookAroundTime;
    }
    public bool Finished { get; private set; }
    float _lookAroundTime;

    float _timer;
    public override void OnEnter()
    {
        controller.AgentPath.StopMovement();

        Finished = false;
        _timer = 0f;

        controller.SetStateText("Mmm...");
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lookAroundTime) Finished = true;
    }
    public override void OnExit()
    {
        Finished = false;
    }
}
