using UnityEngine;

public static class EvadeBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 pursuerPosition, Vector3 pursuerVelocity)
    {
        float distance = Vector3.Distance(position, pursuerPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);
        Vector3 futurePosition = pursuerPosition + pursuerVelocity * predictionTime;
        return FleeBehaviour.Calculate(position, futurePosition, velocity, maxSpeed);
    }
}
