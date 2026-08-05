using UnityEngine;

[System.Serializable]
public class Combat
{
    [SerializeField, Min(0f)] float _attackDamage = 10f;
    [SerializeField, Min(0f)] float _attackRange = 2f;
    [SerializeField, Min(0f)] float _attackCooldown = 1f;

    float _cooldownTimer;

    public void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    public bool InRange(Vector3 from, Vector3 targetPos)
    {
        return Vector3.Distance(from, targetPos) <= _attackRange;
    }

    public bool TryAttack(IDamageable target)
    {
        if (_cooldownTimer > 0f) return false;

        target.TakeDamage(_attackDamage);
        _cooldownTimer = _attackCooldown;

        return true;
    }
}
public interface IDamageable
{
    public void TakeDamage(float damage);
}