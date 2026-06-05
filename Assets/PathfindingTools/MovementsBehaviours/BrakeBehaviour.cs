using UnityEngine;

public static class BrakeBehaviour
{
    public static Vector3 Calculate(Vector3 velocity, float brakeForce)
    {
        return -velocity.normalized * brakeForce;
    }
}
