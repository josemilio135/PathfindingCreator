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
    public virtual void OnEnter()
    {
        //Debug.Log("Enter State");
    }
    public virtual void Update()
    {
        //Debug.Log("Update State");
    }
    public virtual void OnExit()
    {
        //  Debug.Log("Exit State");
    }
}

