using UnityEngine;

public class IdleLeaderState : BaseState<Leader>
{
    public IdleLeaderState(StateMachine fsm, Leader controller, float lookAroundTime, float searchRotationAngle) : base(fsm, controller)
    {
        _lookAroundTime = lookAroundTime;
        _searchRotationAngle = searchRotationAngle;
    }
    public bool Finished { get; private set; }
    float _lookAroundTime;
    float _searchRotationAngle;
    float _timer;
    Quaternion _startRot;
    public override void OnEnter()
    {
        controller.AgentPath.StopMovement();
        Finished = false;
        _timer = 0f;
        _startRot = controller.transform.rotation;

        controller.SetColorFOV("9CA3AF");
        controller.SetStateText("Idle...");
    }
    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lookAroundTime) Finished = true;
        float t = _timer / _lookAroundTime;
        float currentAngle = Mathf.Sin(t * Mathf.PI * 2f) * _searchRotationAngle;
        controller.transform.rotation = _startRot * Quaternion.Euler(0f, currentAngle, 0f);
    }
    public override void OnExit() { }
}