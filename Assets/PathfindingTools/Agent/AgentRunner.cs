using System.Collections.Generic;
using UnityEngine;
using static PathfindingRunner;

public class AgentRunner : MonoBehaviour
{
    [Header("Pathfinding")]
    [SerializeField] SolverType _solverType = SolverType.AStar;
    [SerializeField] NodesContainer _container;

    [Header("Movement")]
    [SerializeField, Min(0f)] float _moveSpeed = 5f;
    [SerializeField, Min(0f)] float _rotationSpeed = 10f;
    [SerializeField, Min(0.01f)] float _nodeReachDistance = 0.2f;

    [Header("Arrive")]
    [SerializeField, Min(0f)] float _slowDownDistance = 3f;

    [Header("Debug")]
    [SerializeField] bool _drawPath = true;
    [SerializeField] Color _pathColor = Color.white;
    [SerializeField] Color _currentNodeColor = Color.yellow;

    PathfindingRunner _pathfinding;
    KinematicMovement _movement;

    List<BaseNode> _currentPath = new();
    int _currentIndex;
    WaypointNode _tempStart;
    WaypointNode _tempEnd;
    public System.Action OnDestinationReached;

    public NodesContainer CurrentContainer => _container;
    public bool IsMoving => _currentPath != null && _currentIndex < _currentPath.Count;
    public Vector3 Velocity => _movement.Velocity;
    public float MoveSpeed => _moveSpeed;
    public float RotationSpeed => _rotationSpeed;

    void Awake()
    {
        _movement = new KinematicMovement(_rotationSpeed);

        _pathfinding = gameObject.AddComponent<PathfindingRunner>();
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

        _currentPath = _pathfinding.FindPath<BaseNode>(transform.position, destination);
        _currentIndex = 0;

        _tempStart.Disconnect(_pathfinding.Container);
        _tempEnd.Disconnect(_pathfinding.Container);

        _tempStart.gameObject.SetActive(false);
        _tempEnd.gameObject.SetActive(false);
    }

    public void StopMovement() => _currentPath?.Clear();

    void Update()
    {
        FollowPath();
    }

    void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_currentIndex >= _currentPath.Count)
        {
            _currentPath.Clear();
            OnDestinationReached?.Invoke();
            return;
        }

        Vector3 nodePos = _currentPath[_currentIndex].Position;
        float speed = _moveSpeed;

        float remainingDistance = RemainingPathDistance();
        if (remainingDistance < _slowDownDistance)
        {
            speed = Mathf.Max(_moveSpeed * (remainingDistance / _slowDownDistance), 0.1f);
        }

        bool arriveDestination =
            _movement.MoveTowardsFlat(transform, nodePos, speed, _nodeReachDistance);

        if (arriveDestination) _currentIndex++;
    }

    float RemainingPathDistance()
    {
        if (_currentPath == null || _currentIndex >= _currentPath.Count) return 0f;

        float distance = Vector3.Distance(transform.position, _currentPath[_currentIndex].Position);

        for (int i = _currentIndex; i < _currentPath.Count - 1; i++)
            distance += Vector3.Distance(_currentPath[i].Position, _currentPath[i + 1].Position);

        return distance;
    }


    Vector3 FindNearestNavegablePos(Vector3 target, float maxSearchRadius = 3f, float stepRadius = 0.25f, int samplesPerRing = 16)
    {
        if (IsPositionWalkable(target)) return target;

        for (float radius = stepRadius; radius <= maxSearchRadius; radius += stepRadius)
        {
            for (int i = 0; i < samplesPerRing; i++)
            {
                float angle = (360f / samplesPerRing) * i;
                Vector3 candidate = target + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                if (IsPositionWalkable(candidate)) return candidate;
            }
        }
        return target;
    }

    bool IsPositionWalkable(Vector3 position)
    {
        float radius = _container.Agent.Radius;
        float height = _container.Agent.Height;

        Vector3 bottom = position + Vector3.up * radius;
        Vector3 top = position + Vector3.up * (height - radius);

        return !Physics.CheckCapsule(bottom, top, radius, _container.Agent.ObstacleMask);
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