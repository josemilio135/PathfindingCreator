using UnityEngine;

public class SearchState : BaseState<Hunter>
{
    Vector3 _searchPos;
    public SearchState(StateMachine fsm, Hunter controller) : base(fsm, controller)
    {
    }
    public void SetSearchPosition(Vector3 position) => _searchPos = position;

    public override void OnEnter()
    {
        controller.IsAlert = false;
        controller.AgentPath.SetDestination(_searchPos);
    }

    public override void Update() { }

    public override void OnExit()
    {
    }
}
