public class ActionNode : BehaviourNode
{
    System.Action _action;
    public ActionNode(System.Action action)
    {
        _action = action;
    }
    public override void Execute()
    {
        _action?.Invoke();
    }
}

