using UnityEngine;

public class FleeState : BaseState<EntityController>
{
    const float RepathTime = 0.25f;

    float _timer;

    public FleeState(StateMachine fsm, EntityController controller) : base(fsm, controller) { }
    public override void OnEnter()
    {
        controller.SetColorFOV("FFD400");
        controller.SetStateText("Fleeing!");

        _timer = 0f;

        UpdateDestination();
    }

    public override void Update()
    {
        if (controller.CurrentEnemyTarget == null)
            return;

        _timer += Time.deltaTime;

        if (_timer >= RepathTime)
        {
            _timer = 0f;
            UpdateDestination();
        }
    }

    void UpdateDestination()
    {
        if (controller.CurrentEnemyTarget == null)
            return;

        Vector3 threatPos = controller.CurrentEnemyTarget.Position;
        Vector3 away = (controller.Position - threatPos).normalized;
        Vector3 destination = controller.Position + away * controller.FleeDistance;

        controller.AgentPath.SetDestination(destination);
    }

    public override void OnExit()
    {
        controller.AgentPath.StopMovement();
    }
}