using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a navigable node graph using Breadth-First Search (BFS).
/// Starting from a seed position, the algorithm samples visible nodes nearby,
/// adds them to the graph, then expands outward from each new node, until no new nodes are discovered.
/// </summary>
public static class NodeGraphBake
{
    /// <summary>
    /// Iterates outward from <paramref name="seedPosition"/>, collecting and
    /// merging visible nodes at each step until the entire reachable area is covered.
    /// Returns the full list of unique node positions that form the graph.
    /// </summary>
    public static List<Vector3> GenerateGraph(
    Vector3 seedPosition,
        float viewRange,
        AgentConfig agent,
        int curvedPrecision,
        float mergeDistance)
    {
        Queue<Vector3> frontier = new();
        List<Vector3> nodesGraph = new();

        frontier.Enqueue(seedPosition);

        while (frontier.Count > 0)
        {
            Vector3 currentNode = frontier.Dequeue();

            IEnumerable<Vector3> visibleNodes =
                 NodeSampler.GetVisibleNodes(
                     currentNode, viewRange,
                     agent,
                     curvedPrecision);

            List<Vector3> mergedNodes =
                NodeSampler.MergeNearbyNodes(visibleNodes, mergeDistance);

            foreach (Vector3 node in mergedNodes)
            {
                if (AlreadyInGraph(nodesGraph, node, mergeDistance)) continue;
                nodesGraph.Add(node);
                frontier.Enqueue(node);
            }
        }

        return nodesGraph;
    }
    /// <summary>
    /// Returns true if a node close enough to <paramref name="node"/> already
    /// exists in the graph. 
    /// Proximity is evaluated on the horizontal plane only (Y is ignored)
    /// So nodes on different floors at the same XZ position are still treated 
    /// as duplicates and wont be explored twice.
    /// </summary>
    static bool AlreadyInGraph(List<Vector3> graph, Vector3 node, float mergeDistance)
    {
        if (mergeDistance <= 0f)
        {
            Debug.LogError("NodeGraphBake: mergeDistance must be greater than zero.");
            return false;
        }

        float sqr = mergeDistance * mergeDistance;

        foreach (Vector3 existing in graph)
        {
            Vector3 flatExisting = existing; flatExisting.y = 0f;
            Vector3 flatNode = node; flatNode.y = 0f;

            if ((flatExisting - flatNode).sqrMagnitude <= sqr) return true;
        }

        return false;
    }
}