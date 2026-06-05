using UnityEngine;

public static class ArriveBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed, float slowingRadius = 2f)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;
        float desiredSpeed = distance < slowingRadius ? maxSpeed * (distance / slowingRadius) : maxSpeed;

        Vector3 desired = toTarget.normalized * desiredSpeed;
        return desired - velocity;
    }
}
