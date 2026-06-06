using UnityEngine;

public class LookAroundState : BaseState<Hunter>
{

    public LookAroundState(StateMachine fsm, Hunter controller, float lookAroundTime, float searchRotationAngle) : base(fsm, controller)
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

        controller.SetStateText("Mmm...");
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lookAroundTime) Finished = true;

        float currentAngle =
            Mathf.PingPong(_timer * (_timer/_lookAroundTime), _searchRotationAngle * 2f) - _searchRotationAngle;

        controller.transform.rotation =
            _startRot * Quaternion.Euler(0f, currentAngle, 0f);

    }
    public override void OnExit()
    {
        // Finished = false;k
    }
}
