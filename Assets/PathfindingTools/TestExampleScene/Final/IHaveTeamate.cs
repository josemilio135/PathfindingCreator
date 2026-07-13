
using UnityEngine;

public interface IHaveTeamate
{
    public Teamates Team { get; }
    public Vector3 Position { get; }
}
public enum Teamates
{
    Blue,
    Red
}