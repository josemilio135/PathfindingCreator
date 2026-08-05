using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    readonly List<IFlockMember> _members = new();

    public void Register(IFlockMember member)
    {
        if (!_members.Contains(member)) _members.Add(member);
    }

    public void Unregister(IFlockMember member) => _members.Remove(member);

    public int GetNeighbors(IFlockMember self, float radius, IFlockMember[] buffer)
    {
        float sqrRadius = radius * radius;
        int count = 0;

        for (int i = 0; i < _members.Count && count < buffer.Length; i++)
        {
            IFlockMember other = _members[i];
            if (other == null || other == self) continue;

            if ((other.Position - self.Position).sqrMagnitude <= sqrRadius)
                buffer[count++] = other;
        }

        return count;
    }
}