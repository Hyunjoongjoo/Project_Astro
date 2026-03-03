using UnityEngine;

[CreateAssetMenu(fileName = "NormalAttackDataSO", menuName = "Scriptable Objects/NormalAttackDataSO")]
public class NormalAttackDataSO : ScriptableObject
{
    [SerializeField] private string _skillId;


    [SerializeField] private AttackType _attackType;
    [SerializeField] private GameObject _effectPrefab;

    public string SkillId => _skillId;
    public AttackType AttackType => _attackType;
    public GameObject EffectPrefab => _effectPrefab;
}
