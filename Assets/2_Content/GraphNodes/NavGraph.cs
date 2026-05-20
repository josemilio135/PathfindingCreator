using System.Collections.Generic;
using UnityEngine;

public class NavGraph : MonoBehaviour
{
    [SerializeField] List<NavNode> nodes = new();
    [SerializeField] Transform startTransform;
    [SerializeField] Transform   endTransform;
    [SerializeField] NavNode startNode;
    [SerializeField] NavNode endNode;
    [SerializeField] float clearanceRadius = 0;
    [SerializeField] LayerMask obstacleMask;


    bool resolveEndPoints()
    {
        startNode = FindClosestVisibleNode(startTransform != null ? startTransform.position : Vector3.zero);
        endNode = FindClosestVisibleNode(endTransform != null ? endTransform.position : Vector3.zero);

        if (startNode == null || endNode == null) return false;
        return true;
    }
    NavNode FindClosestVisibleNode(Vector3 from)
    {
        NavNode closestNode = null;
        float bestDistance = float.MaxValue;

        foreach (NavNode node in nodes)
        {
            if (node == null) continue;
            float distance = Vector3.Distance(from, node.WorldPosition);
            if (distance >= bestDistance) continue;
            if (!Perception.HasLineOfSight_Sphere(from, node.WorldPosition, clearanceRadius, obstacleMask, out RaycastHit hit))
            {
                continue;
            }

            closestNode = node;
            bestDistance = distance;
        }

        return closestNode;
    }


}
