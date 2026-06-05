using UnityEngine;

public static class WanderBehaviour
{
    public static Vector3 Calculate(Vector3 position, Vector3 forward, float maxSpeed, ref Vector3 wanderTarget, float radius = 2f, float distance = 4f, float jitter = 40f)
    {
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)) * jitter * Time.deltaTime;

        wanderTarget = wanderTarget.normalized * radius;

        Vector3 circleCenter = position + forward * distance;
        Vector3 worldTarget = circleCenter + wanderTarget;

        return SeekBehaviour.Calculate(position, worldTarget, Vector3.zero, maxSpeed);
    }
}
