using UnityEngine;

public class LookAroundState : BaseState<Hunter>
{
    public LookAroundState(StateMachine fsm, Hunter controller, float lookAroundTime) : base(fsm, controller)
    {
        _lookAroundTime = lookAroundTime;
    }

    float _lookAroundTime;
    float _currentTime_lk = 0f;
    public override void OnEnter()
    {
        controller.AgentPath.StopMovement();

        _currentTime_lk = 0f;
        controller.IsLookAround = true;
    }

    public override void Update()
    {
        if (_currentTime_lk < _lookAroundTime)
            _currentTime_lk += Time.deltaTime;
        else controller.IsLookAround = false;
    }

    public override void OnExit()
    {
    }
}
