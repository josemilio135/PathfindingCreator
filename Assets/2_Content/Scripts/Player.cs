using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentRunner))]
public class Player : MonoBehaviour
{
    GameInput _inputs;
    bool InputSetDestination => _inputs.Gameplay.MouseLeftClic.WasPressedThisFrame();


    AgentRunner _agent;
    LayerMask _walkableMask;

    private void Awake()
    {
        _inputs = new GameInput();
        _inputs.Enable();
        _agent = GetComponent<AgentRunner>();
        _walkableMask = _agent.CurrentContainer.Agent.WalkableMask;
    }


    private void Update()
    {
        if (InputSetDestination) SetDestination();
    }
    void SetDestination()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 50, _walkableMask))
        {
            _agent.SetDestination(hit.point);
        }
    }
}
