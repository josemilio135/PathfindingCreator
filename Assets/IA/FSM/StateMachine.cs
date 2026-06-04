using System;
using System.Collections.Generic;

public class StateMachine
{
    public StateNode currentState;

    Dictionary<Type, StateNode> nodes = new();
    
    HashSet<ITransition> anyTransition = new();

    public void Update()
    {
        ITransition transition = GetTransition();
        if (transition != null) ChangeState(transition.To);

        currentState.State?.Update();
    }
    public void SetState(IState state)
    {
        //currentState = nodes[state.GetType()];
        currentState = GetOrAddNode(state);
        currentState.State?.OnEnter();
    }
    void ChangeState(IState state)
    {
        if (state == null) return;

        IState previousState = currentState.State;
        IState nextState = nodes[state.GetType()].State;

        previousState?.OnExit();
        nextState?.OnEnter();
        currentState = nodes[state.GetType()];
    }

    public void AddTransition(IState from, IState to, IPredicate condition)
    {
        GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
    }
    public void AddAnyTransition(IState to, IPredicate condition)
    {
        anyTransition.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    StateNode GetOrAddNode(IState state)
    {
        StateNode node = nodes.GetValueOrDefault(state.GetType());

        if (node == null)
        {
            node = new StateNode(state);
            nodes.Add(state.GetType(), node);
        }
        return node;
    }
    ITransition GetTransition()
    {
        foreach (var transition in anyTransition)
        {
            if (transition.Condition.Evaluate()) return transition; //Global
        }
        foreach (var transition in currentState.Transitions)
        {
            if (transition.Condition.Evaluate()) return transition; //Local
        }
        return null;
    }
}
