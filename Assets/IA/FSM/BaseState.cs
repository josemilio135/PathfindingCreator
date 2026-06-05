public abstract class BaseState<T> : IState where T : Controller
{
    protected readonly StateMachine fsm;

    protected readonly T controller;

    //protected readonly Animator animator;

    protected BaseState(StateMachine fsm, T controller)
    {
        this.fsm = fsm;
        this.controller = controller;
        //this.animator = animator;
    }
    public abstract void OnEnter();
    public abstract void Update();
    public abstract void OnExit();
}

