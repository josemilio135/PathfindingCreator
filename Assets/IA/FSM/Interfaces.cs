public interface IPredicate
{
    public bool Evaluate();
}

public interface IState
{
    public void OnEnter();
    public void Update();
    public void OnExit();
}

public interface ITransition
{
    IState To { get; }
    IPredicate Condition { get; }
}
