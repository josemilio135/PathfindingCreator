using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathfindingRunner))]
public abstract class AgentRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] float _rotationSpeed = 5f;
    [SerializeField] float _nodeReachDistance = 0.2f;

    PathfindingRunner _pathfinding;
    List<BaseNode> _currentPath = new();
    int _currentIndex;

    WaypointNode _tempStart;
    WaypointNode _tempEnd;

    protected virtual void Awake()
    {
        _pathfinding = GetComponent<PathfindingRunner>();
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
        _tempStart.transform.position = transform.position;
        _tempEnd.transform.position = destination;

        _tempStart.gameObject.SetActive(true);
        _tempEnd.gameObject.SetActive(true);

        _tempStart.Connect(_pathfinding.Container);
        _tempEnd.Connect(_pathfinding.Container);

        _currentPath = _pathfinding.FindPath<BaseNode>(transform.position, destination);
        _currentIndex = 0;

        _tempStart.Disconnect(_pathfinding.Container);
        _tempEnd.Disconnect(_pathfinding.Container);

        _tempStart.gameObject.SetActive(false);
        _tempEnd.gameObject.SetActive(false);
    }

    protected void FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        if (_currentIndex >= _currentPath.Count)
        {
            _currentPath.Clear();
            OnDestinationReached();
            return;
        }

        if (MoveTowards(_currentPath[_currentIndex].Position, _nodeReachDistance))
            _currentIndex++;
    }

    protected bool MoveTowards(Vector3 target, float stoppingDistance)
    {
        Vector3 flat = new(target.x, transform.position.y, target.z);
        float distance = Vector3.Distance(transform.position, flat);
        if (distance <= stoppingDistance) return true;

        Vector3 direction = (flat - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        transform.position += direction * _moveSpeed * Time.deltaTime;
        return false;
    }

    protected virtual void OnDestinationReached() { }

    #region Gizmos

    [Header("Debug")]
    [SerializeField] bool _drawPath = true;
    [SerializeField] Color _pathColor = Color.white;
    [SerializeField] Color _currentNodeColor = Color.yellow;
    void OnDrawGizmos()
    {
        if (_drawPath && _currentPath != null && _currentPath.Count > 0)
        {
            Gizmos.color = _pathColor;
            for (int i = 0; i < _currentPath.Count - 1; i++)
                Gizmos.DrawLine(_currentPath[i].Position, _currentPath[i + 1].Position);

            if (_currentIndex < _currentPath.Count)
            {
                Gizmos.color = _currentNodeColor;
                Gizmos.DrawSphere(_currentPath[_currentIndex].Position, 0.35f);
            }
        }

        DrawGizmos(_currentPath, _currentIndex);
    }

    protected virtual void DrawGizmos(List<BaseNode> path, int currentIndex) { }
    #endregion
}