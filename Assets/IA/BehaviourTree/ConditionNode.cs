using System;

public class ConditionNode : BehaviourNode
{
    readonly Func<bool> _condition;

    readonly BehaviourNode _falseNode;
    readonly BehaviourNode _trueNode;
    public ConditionNode(Func<bool> condition, BehaviourNode trueNode, BehaviourNode falseNode)
    {
        _condition = condition;
        _trueNode = trueNode;
        _falseNode = falseNode;
    }
    public override void Execute()
    {
        if (_condition.Invoke()) _trueNode.Execute();
        else _falseNode.Execute();
    }
}

