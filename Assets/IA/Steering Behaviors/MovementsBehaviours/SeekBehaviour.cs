using UnityEngine;

public static class SeekBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 desired = (targetPos - position).normalized * maxSpeed;
        return desired - velocity;
    }
}
