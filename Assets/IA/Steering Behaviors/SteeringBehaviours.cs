using UnityEngine;

public static class SteeringBehaviours
{
    public static Vector3 Seek(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 desired = (targetPos - position).normalized * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Flee(Vector3 position, Vector3 threatPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 desired = (position - threatPos).normalized * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Arrive(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed, float slowingRadius = 5f)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;
        float desiredSpeed = distance < slowingRadius ?
                              maxSpeed * (distance / slowingRadius) : maxSpeed;

        Vector3 desired = toTarget.normalized * desiredSpeed;
        return desired - velocity;
    }
    public static Vector3 Pursuit(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 preyPosition, Vector3 preyVelocity)
    {
        float distance = Vector3.Distance(position, preyPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);

        Vector3 futurePosition = preyPosition + preyVelocity * predictionTime;

        return Seek(position, futurePosition, velocity, maxSpeed);
    }

    public static Vector3 Evade(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 pursuerPosition, Vector3 pursuerVelocity)
    {
        float distance = Vector3.Distance(position, pursuerPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);

        Vector3 futurePosition = pursuerPosition + pursuerVelocity * predictionTime;

        return Flee(position, futurePosition, velocity, maxSpeed);
    }

    public static Vector3 Wander(Vector3 position, Vector3 forward, float maxSpeed, ref Vector3 wanderTarget, float radius = 2f, float distance = 4f, float jitter = 40f)
    {
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f))
            * jitter
            * Time.deltaTime;

        wanderTarget = wanderTarget.normalized * radius;

        Vector3 circleCenter = position + forward * distance;
        Vector3 worldTarget = circleCenter + wanderTarget;

        return Seek(position, worldTarget, Vector3.zero, maxSpeed);
    }
    public static Vector3 Brake(Vector3 velocity, float brakeForce)
    {
        return -velocity.normalized * brakeForce;
    }
}