using UnityEngine;

public class DeadState : BaseState<EntityController>
{
    float timer;
    Renderer render;
    Color defaultColor;
    public DeadState(StateMachine fsm, EntityController controller) : base(fsm, controller)
    {
        render = controller.GetComponentInChildren<Renderer>();
        if (render == null) return;

        defaultColor = render.GetColor();
    }


    public override void OnEnter()
    {
        timer = 5f;
        controller.AgentPath.StopMovement();
        controller.SetStateText("Dead");
        controller.SetColorFOV("808080");
        render?.SetColor(Color.gray);
    }
    public override void OnExit()
    {
        render?.SetColor(defaultColor);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0) controller.Revive();
    }
}