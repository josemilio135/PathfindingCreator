public class AttackState : BaseState<EntityController>
{
    public AttackState(StateMachine fsm, EntityController controller) : base(fsm, controller) { }
    public override void OnEnter()
    {
        controller.SetColorFOV("FF3B00");
        controller.SetStateText("Attacking!");
    }
    public override void Update()
    {
        if (controller.CurrentEnemyTarget == null) return;
        if (!controller.InAttackRange())
        {
            controller.AgentPath.SetDestination(controller.CurrentEnemyTarget.Position);
            return;
        }
        controller.AgentPath.StopMovement();
        if (controller.CurrentEnemyTarget is IDamageable target)
            controller.Combat.TryAttack(target);
    }
    public override void OnExit() => controller.AgentPath.StopMovement();
}