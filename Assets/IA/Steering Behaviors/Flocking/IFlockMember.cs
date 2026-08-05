using UnityEngine;

public interface IFlockMember
{
    Vector3 Position { get; }
    Vector3 Velocity { get; }
    int GroupId { get; }
}