using UnityEngine;
public class PatrolState : BaseState<Hunter>
{
    AgentRunner _agent;
    Transform[] _waypoints;
    int _waypointIndex;
    int _waypointDir = 1;

    public PatrolState(StateMachine fsm, Hunter controller, Transform waypointsContainer) : base(fsm, controller)
    {
        _agent = controller.AgentPath;
        GetWayPoints(waypointsContainer);
    }

    public override void OnEnter()
    {
        AdvanceWaypoint();
        GoToCurrentWaypoint();

        controller.SetStateText("...");
    }

    public override void Update() { }

    public override void OnExit() { }

    void GetWayPoints(Transform waypointsContainer)
    {
        if (waypointsContainer != null)
        {
            _waypoints = new Transform[waypointsContainer.childCount];
            for (int i = 0; i < waypointsContainer.childCount; i++)
                _waypoints[i] = waypointsContainer.GetChild(i);
        }
        else _waypoints = System.Array.Empty<Transform>();
    }

    void GoToCurrentWaypoint()
    {
        if (_waypoints.Length == 0) return;
        _agent.SetDestination(_waypoints[_waypointIndex].position);
    }

    void AdvanceWaypoint()
    {
        if (_waypoints.Length == 0) return;
        if (controller.PatrolPingPong)
        {
            _waypointIndex += _waypointDir;
            if (_waypointIndex >= _waypoints.Length)
            {
                _waypointDir = -1;
                _waypointIndex = _waypoints.Length - 2;
            }
            else if (_waypointIndex < 0)
            {
                _waypointDir = 1;
                _waypointIndex = 1;
            }
        }
        else _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
    }
}