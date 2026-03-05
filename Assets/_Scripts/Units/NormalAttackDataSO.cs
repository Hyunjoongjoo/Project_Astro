using UnityEngine;

[CreateAssetMenu(fileName = "NormalAttackDataSO", menuName = "Scriptable Objects/NormalAttackDataSO")]
public class NormalAttackDataSO : ScriptableObject
{
    [SerializeField] private string _skillId;


    [SerializeField] private AttackType _attackType;
    [SerializeField] private SkillType _category;
    [SerializeField] private GameObject _effectPrefab;

    public string SkillId => _skillId;
    public SkillType Category => _category;
    public AttackType AttackType => _attackType;
    public GameObject EffectPrefab => _effectPrefab;
}
