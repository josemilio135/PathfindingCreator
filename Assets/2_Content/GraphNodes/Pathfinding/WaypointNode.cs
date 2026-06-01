public class WaypointNode : BaseNode
{
    public void Connect(NodesContainer container)
    {
        Disconnect(container);

        foreach (BaseNode node in container.Nodes)
        {
            if (node == null || node == this) continue;

            bool hasLOS = Perception.HasLineOfSight_Capsule(
                Position, node.Position,
                container.Agent.Radius,
                container.Agent.Height,
                container.Agent.ObstacleMask);

            if (!hasLOS) continue;

            AddNeighbor(node);
            node.AddNeighbor(this);
        }

        container.Nodes.Add(this);
    }

    void Disconnect(NodesContainer container)
    {
        foreach (BaseNode neighbor in Neighbors)
            neighbor.RemoveNeighbor(this);

        ClearNeighboirs();
        container.Nodes.Remove(this);
    }
}