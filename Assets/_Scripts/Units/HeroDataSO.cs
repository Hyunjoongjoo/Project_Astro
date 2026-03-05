using UnityEngine;

[CreateAssetMenu(fileName = "HeroDataSO", menuName = "Scriptable Objects/HeroDataSO")]
public class HeroDataSO : ScriptableObject
{
    [SerializeField] private NormalAttackDataSO _normalAttack;
    [SerializeField] private string _heroId;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private SkillDataSO _normalSkill;
    [SerializeField] private float _summonCooldown;
    [SerializeField] private float _attackRange;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _searchRange;
    [SerializeField] private float _healAmount;

    public NormalAttackDataSO NormalAttack => _normalAttack;
    public string HeroID => _heroId;
    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public SkillDataSO NormalSkill => _normalSkill;
    public float SummonCooldown => _summonCooldown;
    public float AttackRange => _attackRange;
    public float AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;
    public float SearchRange => _searchRange;
    public float HealAmount => _healAmount;
}
