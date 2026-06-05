using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentRunner))]
public class Player : MonoBehaviour
{
    bool InputSetDestination => Mouse.current.leftButton.wasPressedThisFrame;
    bool InputStopMove => Keyboard.current.escapeKey.wasPressedThisFrame;


    AgentRunner _agent;
    LayerMask _walkableMask;

    private void Awake()
    {
        _agent = GetComponent<AgentRunner>();
        _walkableMask = _agent.CurrentContainer.Agent.WalkableMask;
    }


    private void Update()
    {
        if (InputSetDestination) SetDestination();
        if (InputStopMove) _agent.StopMovement();
    }
    void SetDestination()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500, _walkableMask))
        {
            _agent.SetDestination(hit.point);
        }
    }
}
