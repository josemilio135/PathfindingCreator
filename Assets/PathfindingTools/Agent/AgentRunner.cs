using System.Collections.Generic;
using UnityEngine;
using static PathfindingRunner;

public class AgentRunner : Steerings
{
    [Header("Pathfinding")]
    [SerializeField] SolverType _solverType = SolverType.AStar;
    [SerializeField] NodesContainer _container;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] float _nodeReachDistance = 0.2f;

    [Header("Arrive")]
    [SerializeField, Min(0f)] float _slowDownDistance = 3f;

    [Header("Debug")]
    [SerializeField] bool _drawPath = true;
    [SerializeField] Color _pathColor = Color.white;
    [SerializeField] Color _currentNodeColor = Color.yellow;

    PathfindingRunner _pathfinding;

    List<BaseNode> _currentPath = new();
    int _currentIndex;
    WaypointNode _tempStart;
    WaypointNode _tempEnd;

    public System.Action OnDestinationReached;

    public NodesContainer CurrentContainer => _container;
    public bool IsMoving => _currentPath != null && _currentIndex < _currentPath.Count;
    public Vector3 Velocity => Controller.Velocity;
    public float StopDistance { get; set; } = 0f;

    protected override void Awake()
    {
        base.Awake();

        _pathfinding = GetComponent<PathfindingRunner>();
        if (!_pathfinding) _pathfinding = gameObject.AddComponent<PathfindingRunner>();

        _pathfinding.CurrentSolverType = _solverType;
        _pathfinding.Container = _container;

        _tempStart = CreateTempNode("Start");
        _tempEnd = CreateTempNode("End");
    }

    WaypointNode CreateTempNode(string label)
    {
        GameObject go = new($"[TempNode_{name}_{label}]");
        WaypointNode node = go.AddComponent<WaypointNode>();
        go.SetActive(false);
        return node;
    }

    public void SetDestination(Vector3 destination)
    {
        _tempStart.gameObject.SetActive(false);
        _tempEnd.gameObject.SetActive(false);

        destination = FindNearestNavegablePos(destination);

        _pathfinding.CurrentSolverType = _solverType;

        _tempStart.transform.position = transform.position;
        _tempEnd.transform.position = destination;

        _tempEnd.gameObject.SetActive(true);

        if (HasDirectLOS(destination))
        {
            _currentPath.Clear();
            _currentPath.Add(_tempEnd);
            _currentIndex = 0;
            return;
        }

        _tempStart.gameObject.SetActive(true);

        _tempStart.Connect(_pathfinding.Container);
        _tempEnd.Connect(_pathfinding.Container);

        if (_tempStart.Neighbors.Count == 0) _tempStart.Disconnect(_pathfinding.Container);
        if (_tempEnd.Neighbors.Count == 0) _tempEnd.Disconnect(_pathfinding.Container);

        _currentPath = _pathfinding.FindPath<BaseNode>(transform.position, destination);
        _currentIndex = 0;

        _tempStart.Disconnect(_pathfinding.Container);
        _tempEnd.Disconnect(_pathfinding.Container);

        _tempStart.gameObject.SetActive(false);
        _tempEnd.gameObject.SetActive(false);
    }

    public void StopMovement() => _currentPath?.Clear();

    protected override void Update()
    {
        FollowPath();
        base.Update();
    }

    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_currentIndex >= _currentPath.Count)
        {
            StopMovement();
            OnDestinationReached?.Invoke();
            return;
        }

        if (HasReachedNode(_currentPath[_currentIndex].Position))
        {
            _currentIndex++;

            if (_currentIndex >= _currentPath.Count)
            {
                StopMovement();
                OnDestinationReached?.Invoke();
                return;
            }
        }

        float remainingDistance = RemainingPathDistance();

        if (StopDistance > 0f && remainingDistance <= StopDistance)
        {
            StopMovement();
            OnDestinationReached?.Invoke();
        }
    }

    protected override Vector3 CalculateSteering()
    {
        if (_currentPath == null || _currentIndex >= _currentPath.Count)
            return Vector3.zero;

        Vector3 targetPos = _currentPath[_currentIndex].Position;
        targetPos.y = transform.position.y;

        float remainingDistance = RemainingPathDistance();

        if (remainingDistance <= _slowDownDistance)
        {
            Vector3 direction = targetPos - transform.position;

            return SteeringCalculator.Arrive(
                direction, remainingDistance,
                Controller.Velocity, Controller.MaxSpeed, _slowDownDistance);
        }

        return SteeringCalculator.Seek(
            transform.position, targetPos,
            Controller.Velocity, Controller.MaxSpeed);
    }

    float RemainingPathDistance()
    {
        if (_currentPath == null || _currentIndex >= _currentPath.Count) return 0f;

        float distance = Vector3.Distance(
            transform.position, _currentPath[_currentIndex].Position);

        for (int i = _currentIndex; i < _currentPath.Count - 1; i++)
        {
            distance += Vector3.Distance(
                _currentPath[i].Position, _currentPath[i + 1].Position);
        }

        return distance;
    }

    Vector3 FindNearestNavegablePos(Vector3 target)
    {
        return AgentPhysics.FindNearestValidPosition(
            target,
            _container.MaxNodeRadius, _container.Agent.Radius,
            IsPositionWalkable);

    }

    bool IsPositionWalkable(Vector3 position)
    {
        AgentConfig agent = _container.Agent;

        if (!AgentPhysics.IsWalkable(position, agent.Radius, agent.Height, agent.ObstacleMask, agent.WalkableMask))
            return false;

        BaseNode closest = _container.FindClosestNode(position);
        if (closest == null) return false;

        return Perception.HasLineOfSight_Capsule(position, closest.Position, agent.Radius, agent.Height, agent.ObstacleMask)
            && Perception.HasLineOfSight_Capsule(closest.Position, position, agent.Radius, agent.Height, agent.ObstacleMask);
    }

    bool HasReachedNode(Vector3 nodePos)
    {
        Vector2 flatPos = new(transform.position.x, transform.position.z);
        Vector2 flatNode = new(nodePos.x, nodePos.z);

        return Vector2.Distance(flatPos, flatNode) <= _nodeReachDistance;
    }

    public bool HasDirectLOS(Vector3 destination)
    {
        return Perception.HasLineOfSight_Capsule(
                         transform.position, destination,
                         _container.Agent.Radius,
                          _container.Agent.Height,
                         _container.Agent.ObstacleMask);
    }

    #region Gizmos
    void OnDrawGizmos()
    {
        if (!_drawPath || _currentPath == null || _currentPath.Count == 0) return;

        Gizmos.color = _pathColor;
        for (int i = 0; i < _currentPath.Count - 1; i++)
            Gizmos.DrawLine(_currentPath[i].Position, _currentPath[i + 1].Position);

        if (_currentIndex < _currentPath.Count)
        {
            Gizmos.color = _currentNodeColor;
            Gizmos.DrawSphere(_currentPath[_currentIndex].Position, 0.35f);
        }
    }
    #endregion
}