using UnityEngine;

[CreateAssetMenu(fileName = "HeroDataSO", menuName = "Scriptable Objects/HeroDataSO")]
public class HeroDataSO : ScriptableObject
{
    [SerializeField] private string _heroId;
    [SerializeField] private NormalAttackDataSO _normalAttack;
    [SerializeField] private SkillDataSO _normalSkill;
    [SerializeField] private float _attackRange;

    public string HeroID => _heroId;
    public NormalAttackDataSO NormalAttack => _normalAttack;
    public SkillDataSO NormalSkill => _normalSkill;
    public float AttackRange => _attackRange;

}
