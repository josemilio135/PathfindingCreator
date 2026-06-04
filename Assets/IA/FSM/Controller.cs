using UnityEngine;

public abstract class Controller : MonoBehaviour
{
    /* stateMachine.AddTransition(a, b, new FuncPredicate(() => Predicate())); */
    /* stateMachine.AddAnyTransition(a, new FuncPredicate(() => ShouldGetHit())); */

    protected StateMachine stateMachine;

    private void Update()
    {
        stateMachine.Update();
    }
    protected virtual void Start()
    {
        // if (TryGetComponent(out Animator animator)) _animator = animator;

        stateMachine = new StateMachine();

        CreateStates();
        SetInitialState();
        SetTransitions();
    }
    protected void At(IState from, IState to, IPredicate condition)
    {
        stateMachine.AddTransition(from, to, condition);
    }
    protected void Any(IState to, IPredicate condition)
    {
        stateMachine.AddAnyTransition(to, condition);
    }

    protected abstract void SetInitialState();  // stateMachine.SetState(idleState);
    protected abstract void CreateStates();
    //var idleState = new IdleState(stateMachine, this);
    protected abstract void SetTransitions();

}
