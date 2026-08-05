using UnityEngine;

[System.Serializable]
public class Life
{
    [SerializeField] float _max = 100f;
    [SerializeField, Range(0f, 1f)] float weaknessThreshold = .3f;
    float _current;

    public float Max => _max;
    public float Current => _current;
    public float Percent => _max > 0f ? _current / _max : 0f;

    public bool IsWeak => Percent <= weaknessThreshold;
    public bool IsDead => _current <= 0f;

    public void Init() => _current = _max;
    public void TakeDamage(float amount) => _current = Mathf.Max(0f, _current - amount);
    public void Heal(float amount) => _current = Mathf.Min(_max, _current + amount);
}