using UnityEngine;

public class RetreatState : BaseState<EntityController>
{
    const float RetreatRadius = 6f;
    Vector3 _destination;

    public RetreatState(StateMachine fsm, EntityController controller) : base(fsm, controller) { }

    public override void OnEnter()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        _destination = controller.Position + new Vector3(randomDir.x, 0f, randomDir.y) * RetreatRadius;

        controller.AgentPath.SetDestination(_destination);

        controller.SetStateText("Retreating...");
        controller.SetColorFOV("FFD400");
    }
    public override void Update() { }
    public override void OnExit() => controller.AgentPath.StopMovement();
}