using UnityEngine;
using UnityEngine.InputSystem;
public class LeadersController : MonoBehaviour
{
    [SerializeField] Leader[] _leaders;
    int _currentIndex;
    bool InputSetDestination => Mouse.current.leftButton.wasPressedThisFrame;
    bool InputStopMove => Keyboard.current.escapeKey.wasPressedThisFrame;
    bool InputNextLeader => Keyboard.current.eKey.wasPressedThisFrame;
    bool InputPrevLeader => Keyboard.current.qKey.wasPressedThisFrame;
    Leader Current => _leaders[_currentIndex];
    void Update()
    {
        if (InputNextLeader) SwitchLeader(1);
        if (InputPrevLeader) SwitchLeader(-1);
        if (InputSetDestination) SetDestination();
        if (InputStopMove) Current.AgentPath.StopMovement();
    }
    void SwitchLeader(int direction)
    {
        _currentIndex = (_currentIndex + direction + _leaders.Length) % _leaders.Length;
    }
    void SetDestination()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        LayerMask walkable = Current.AgentPath.CurrentContainer.Agent.WalkableMask;
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, walkable))
            Current.RequestDestination(hit.point);
    }
}