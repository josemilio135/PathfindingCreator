using System.Collections.Generic;
using UnityEngine;
public class FlockAgent : SteeringAgent
{
    [HideInInspector] public FlockManager manager;

    [Header("Flocking Weight Settings")]
    [Range(0f, 3f)][SerializeField] float _weightSeparation = 1.5f;
    [Range(0f, 3f)][SerializeField] float _weightAlignment = 1f;
    [Range(0f, 3f)][SerializeField] float _weightCohesion = 1f;

    [SerializeField] float _neighborRadius = 5f;
    [SerializeField] float _separationRadius = 2f;
    [SerializeField] bool _accumulate = true;
    [SerializeField] float _groupSpeed = 1f;
    public void Initialize(FlockManager manager)
    {
        this.manager = manager;
    }
    void Update()
    {
        ApplyMovement();
        if (manager != null) transform.position = manager.WrapPosition(transform.position);
    }
    public void DoFlocking()
    {
        if (manager == null) return;

       // if (!_accumulate) AddForce(Wander() * _groupSpeed);

        List<FlockAgent> neighbors = manager.GetNeighbors(this, _neighborRadius);
        List<FlockAgent> closeNeighbors = manager.GetNeighbors(this, _separationRadius);

        if (closeNeighbors.Count > 0)
        {
            AddForce(CalculateSeparation(closeNeighbors) * _weightSeparation);
        }

        if (neighbors.Count > 0)
        {
            AddForce(CalculateAlignment(neighbors) * _weightAlignment);
            AddForce(CalculateCohesion(neighbors) * _weightCohesion);
        }
    }
    private Vector3 CalculateSeparation(List<FlockAgent> closeNeighbors)
    {
        Vector3 separationForce = Vector3.zero;

        foreach (FlockAgent neighbor in closeNeighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.transform.position);

            if (distance < 0.001f) continue;

            //separationForce += Flee(neighbor.transform.position) / distance;
        }

        return separationForce;
    }

    private Vector3 CalculateAlignment(List<FlockAgent> neighbors)
    {
        Vector3 averageVelocity = Vector3.zero;

        foreach (FlockAgent neighbor in neighbors)
        {
            averageVelocity += neighbor.Velocity;
        }

        averageVelocity /= neighbors.Count;

        return averageVelocity - Velocity;
    }

    private Vector3 CalculateCohesion(List<FlockAgent> neighbors)
    {
        Vector3 centerOfMass = Vector3.zero;

        foreach (FlockAgent neighbor in neighbors)
        {
            centerOfMass += neighbor.transform.position;
        }

        centerOfMass /= neighbors.Count;

        // return Seek(centerOfMass);
        return Vector3.zero;
    }
}

