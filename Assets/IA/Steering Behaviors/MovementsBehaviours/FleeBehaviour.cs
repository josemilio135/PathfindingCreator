using UnityEngine;

public static class FleeBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 threatPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 desired = (position - threatPos).normalized * maxSpeed;
        return desired - velocity;
    }
}
