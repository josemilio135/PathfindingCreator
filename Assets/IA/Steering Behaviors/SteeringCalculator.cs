using UnityEngine;
public static class SteeringCalculator
{
    const float MinDistance = 0.0001f;

    #region Seek
    public static Vector3 Seek(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;

        if (distance < MinDistance) return -velocity;

        Vector3 desired = (toTarget / distance) * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Seek(Vector3 position, Vector3 velocity, float maxSpeed, SteeringController target)
        => Seek(position, target.Position, velocity, maxSpeed);
    #endregion

    #region Arrive
    public static Vector3 Arrive(Vector3 position, Vector3 targetPos, Vector3 velocity, float maxSpeed, float slowingRadius = 2f)
    {
        Vector3 toTarget = targetPos - position;
        return Arrive(toTarget, toTarget.magnitude, velocity, maxSpeed, slowingRadius);
    }

    public static Vector3 Arrive(Vector3 direction, float distance, Vector3 velocity, float maxSpeed, float slowingRadius = 2f)
    {
        if (distance < MinDistance) return -velocity;

        float desiredSpeed = distance < slowingRadius ? maxSpeed * (distance / slowingRadius) : maxSpeed;
        Vector3 desired = (direction / distance) * desiredSpeed;
        return desired - velocity;
    }

    public static Vector3 Arrive(Vector3 position, Vector3 velocity, float maxSpeed, SteeringController target, float slowingRadius = 2f)
        => Arrive(position, target.Position, velocity, maxSpeed, slowingRadius);
    #endregion

    #region Flee
    public static Vector3 Flee(Vector3 position, Vector3 threatPos, Vector3 velocity, float maxSpeed)
    {
        Vector3 away = position - threatPos;
        float distance = away.magnitude;

        if (distance < MinDistance) return Vector3.zero;

        Vector3 desired = (away / distance) * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Flee(Vector3 position, Vector3 velocity, float maxSpeed, SteeringController threat)
        => Flee(position, threat.Position, velocity, maxSpeed);
    #endregion

    #region Pursuit
    public static Vector3 Pursuit(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 preyPosition, Vector3 preyVelocity)
    {
        float distance = Vector3.Distance(position, preyPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);
        Vector3 futurePosition = preyPosition + preyVelocity * predictionTime;
        return Seek(position, futurePosition, velocity, maxSpeed);
    }

    public static Vector3 Pursuit(Vector3 position, Vector3 velocity, float maxSpeed, SteeringController prey)
        => Pursuit(position, velocity, maxSpeed, prey.Position, prey.Velocity);
    #endregion

    #region Evade
    public static Vector3 Evade(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 pursuerPosition, Vector3 pursuerVelocity)
    {
        float distance = Vector3.Distance(position, pursuerPosition);
        float predictionTime = distance / Mathf.Max(maxSpeed, 0.01f);
        Vector3 futurePosition = pursuerPosition + pursuerVelocity * predictionTime;
        return Flee(position, futurePosition, velocity, maxSpeed);
    }

    public static Vector3 Evade(Vector3 position, Vector3 velocity, float maxSpeed, SteeringController pursuer)
        => Evade(position, velocity, maxSpeed, pursuer.Position, pursuer.Velocity);
    #endregion

    #region Wander
    public static Vector3 Wander(Vector3 position, Vector3 forward, Vector3 velocity, float maxSpeed, ref Vector3 wanderTarget, float radius = 2f, float distance = 4f, float jitter = 40f)
    {
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)) * jitter * Time.deltaTime;

        wanderTarget = wanderTarget.normalized * radius;

        Vector3 circleCenter = position + forward * distance;
        Vector3 worldTarget = circleCenter + wanderTarget;

        return Seek(position, worldTarget, velocity, maxSpeed);
    }
    #endregion

    #region Brake
    public static Vector3 Brake(Vector3 velocity, float brakeForce)
    {
        return -velocity.normalized * brakeForce;
    }
    #endregion
}