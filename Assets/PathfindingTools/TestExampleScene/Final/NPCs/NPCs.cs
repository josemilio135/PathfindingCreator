using UnityEngine;

public class NPCs : Controller, IHaveTeamate
{
    [SerializeField] Teamates _team;

    public Teamates Team => _team;
    public Vector3 Position => transform.position;

    protected override void CreateStates()
    { 
    } 
    protected override void SetInitialState()
    { 
    } 
    protected override void SetTransitions()
    { 
    }


}
