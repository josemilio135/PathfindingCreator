using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    [SerializeField] FlockAgent _agentPrefab;
    [SerializeField] int _maxAgents = 30;

    [Header("Spawn Area")]
    [SerializeField] public Vector2 areaSize = new Vector2(40f, 40f);
    [Space]
    [SerializeField] public bool useCircle = false;
    [SerializeField] public float radius = 20f;
    [Space]
    [SerializeField] bool _showGizmos = true;

    public List<FlockAgent> Agents { get; private set; } = new();
    Vector2 halfSize;

    void Start()
    {
        halfSize = areaSize * 0.5f;
        SpawnAgents();
    }

    void SpawnAgents()
    {
        if (_agentPrefab == null) return;

        for (int i = 0; i < _maxAgents; i++)
        {
            Vector3 spawnPos = Vector3.zero;

            if (useCircle)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                spawnPos = transform.position + new Vector3(circle.x, 0f, circle.y);
            }
            else
            {
                spawnPos = transform.position + new Vector3(
                    Random.Range(-halfSize.x, halfSize.x), 0f, Random.Range(-halfSize.y, halfSize.y));
            }

            var agent = Instantiate(_agentPrefab, spawnPos, Quaternion.identity, transform);
            agent.Initialize(this);

            Agents.Add(agent);
        }
    }

    public List<FlockAgent> GetNeighbors(FlockAgent agent, float radius)
    {
        List<FlockAgent> neighbors = new List<FlockAgent>();

        foreach (var other in Agents)
        {
            if (other == agent) continue;

            if (Vector3.Distance(agent.transform.position, other.transform.position) <= radius)
            {
                neighbors.Add(other);
            }
        }

        return neighbors;
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        Vector3 center = transform.position;

        if (useCircle)
        {
            Vector3 offset = position - center;
            if (offset.magnitude > radius)
            {
                offset = offset.normalized * -radius;
                position = center + offset;
            }

            return position;
        }
        
        if (position.x > center.x + halfSize.x) position.x = center.x - halfSize.x;
        else if (position.x < center.x - halfSize.x) position.x = center.x + halfSize.x;

        if (position.z > center.z + halfSize.y) position.z = center.z - halfSize.y;
        else if (position.z < center.z - halfSize.y) position.z = center.z + halfSize.y;

        return position;
    }

    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        Gizmos.color = Color.yellow;

        if (useCircle)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 0f, areaSize.y));
        }
    }
}


