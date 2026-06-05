using UnityEngine;

public static class PursuitBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 preyPosition, Vector3 preyVelocity)
    {
        float distance = Vector3.Distance(position, preyPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);
        Vector3 futurePosition = preyPosition + preyVelocity * predictionTime;
        return SeekBehaviour.Calculate(position, futurePosition, velocity, maxSpeed);
    }
}
