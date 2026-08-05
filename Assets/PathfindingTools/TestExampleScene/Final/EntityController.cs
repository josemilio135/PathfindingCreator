using TMPro;
using UnityEngine;
public abstract class EntityController : Controller, IHaveTeamate, IDamageable
{
    [SerializeField] protected Teamates _team;
    [SerializeField] VisionFOV _vision;
    [SerializeField] Life _life;
    [SerializeField] Combat _combat;
    [Space]
    [SerializeField, Min(0f)] float _fleeDistance = 8f;
    [Space]
    [SerializeField] TMP_Text _stateText;
    [SerializeField] TMP_Text _healthText;
    public Teamates Team => _team;
    public Vector3 Position => transform.position;
    public Vector3 LastKnownPos { get; set; }
    public AgentRunner AgentPath { get; protected set; }
    public IHaveTeamate CurrentEnemyTarget { get; private set; }
    public Life Health => _life;
    public Combat Combat => _combat;
    public float FleeDistance => _fleeDistance;
    public bool CanBeTarget => !Health.IsDead;

    protected virtual void Initialice()
    {
        AgentPath = GetComponent<AgentRunner>();
        _life.Init();

        _vision.Initialize();

        if (TryGetComponent<FlockingSteering>(out var flocking))
            flocking.SetGroupId((int)_team);
    }
    protected override void Update()
    {
        base.Update();
        _combat.Update();
        UpdateHealthText();
    }
    public void TakeDamage(float damage) => _life.TakeDamage(damage);
    public void Revive() => _life.Init();
    void UpdateHealthText()
    {
        if (_healthText)
            _healthText.text = $"{Mathf.CeilToInt(Health.Current)} / {Mathf.CeilToInt(Health.Max)} HP";
    }
    protected bool FindEnemy()
    {
        IHaveTeamate target = CurrentEnemyTarget;

        bool found = _vision.FindEnemy(transform, _team, ref target);

        CurrentEnemyTarget = target;

        return found;
    }
    public bool InAttackRange()
    {
        return CurrentEnemyTarget != null &&
            _combat.InRange(Position, CurrentEnemyTarget.Position);
    }

    public void SetColorFOV(string hexadecimal, float alpha = .2f)
    {
        _vision.SetFovColor(hexadecimal, alpha);
    }

    public void SetStateText(string text)
    {
        if (_stateText)
        {
            _stateText.text = text;
        }
    }

    protected virtual void OnValidate() => _vision.Refresh();
}
