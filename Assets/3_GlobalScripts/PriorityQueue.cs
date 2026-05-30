using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> _heap = new();
    readonly Dictionary<T, int> _indexes = new();
    public int Count => _heap.Count;

    public void Enqueue(T item, float priority)
    {
        _heap.Add((item, priority));

        int index = _heap.Count - 1;
        _indexes[item] = index;

        HeapifyUp(index);
    }
    public T Dequeue()
    {
        if (_heap.Count == 0) return default;

        T top = _heap[0].item;

        int last = _heap.Count - 1;

        _heap[0] = _heap[last];
        _heap.RemoveAt(last);

        if (_heap.Count > 0) HeapifyDown(0);

        return top;
    }

    public bool Contains(T item) => _indexes.ContainsKey(item);

    public void UpdatePriority(T item, float newPriority)
    {
        if (!_indexes.TryGetValue(item, out int index)) return;

        _heap[index] = (item, newPriority);

        HeapifyUp(index);
        HeapifyDown(index);
    }

    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;

            if (_heap[parent].priority <= _heap[i].priority) break;

            Swap(i, parent);

            i = parent;
        }
    }

    private void HeapifyDown(int i)
    {
        int n = _heap.Count;

        while (true)
        {
            int smallest = i;

            int left = 2 * i + 1;
            int right = 2 * i + 2;

            if (left < n && _heap[left].priority < _heap[smallest].priority)
                smallest = left;

            if (right < n && _heap[right].priority < _heap[smallest].priority)
                smallest = right;

            if (smallest == i) break;

            Swap(i, smallest);

            i = smallest;
        }
    }
    private void Swap(int a, int b)
    {
        (_heap[a], _heap[b]) = (_heap[b], _heap[a]);

        _indexes[_heap[a].item] = a;
        _indexes[_heap[b].item] = b;
    }
    public void Clear()
    {
        _heap.Clear();
        _indexes.Clear();
    }
}