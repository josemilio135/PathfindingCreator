using UnityEngine;

public class KinematicMovement
{
    float _rotationSpeed;

    public KinematicMovement(float rotationSpeed)
    {
        _rotationSpeed = rotationSpeed;
    }
    void Rotate(Transform transform, Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }


    #region Move 3D
    public void Move(Transform transform, Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        Rotate(transform, direction);
        transform.position += direction * speed * Time.deltaTime;
    }
    public bool MoveTowards(Transform transform, Vector3 target, float speed, float stoppingDistance)
    {
        if (Vector3.Distance(transform.position, target) <= stoppingDistance) return true;
        Move(transform, target, speed);
        return false;
    }
    #endregion

    #region Move Flat
    public void MoveFlat(Transform transform, Vector3 target, float speed)
    {
        Vector3 flatTarget = new(target.x, transform.position.y, target.z);
        Vector3 direction = (flatTarget - transform.position).normalized;
        Rotate(transform, direction);
        transform.position += direction * speed * Time.deltaTime;
    }
    public bool MoveTowardsFlat(Transform transform, Vector3 target, float speed, float stoppingDistance)
    {
        Vector3 flatTarget = new(target.x, transform.position.y, target.z);
        float distanceXZ = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(flatTarget.x, flatTarget.z));

        if (distanceXZ <= stoppingDistance) return true;
        MoveFlat(transform, target, speed);
        return false;
    }
    #endregion
}