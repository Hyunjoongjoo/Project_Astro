using UnityEngine;

public enum SkillType
{
    normal_attack,
    normal_skill,
    augment_skill,
    augment_skill_enhance
}

public abstract class SkillDataSO : ScriptableObject
{
    [Header("기본 설정")]
    [SerializeField] private string _skillId;
    [SerializeField] private SkillType _skillType;
    [SerializeField] private float _initCooldown;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _skillRange;
    [SerializeField] private GameObject _effectPrefab;

    public abstract IHeroSkill CreateSkillComponent(GameObject owner);

    public virtual SkillRuntimeData CreateRuntimeData()
    {
        return new SkillRuntimeData
        {
            Cooldown = _cooldown,
            SkillRange = _skillRange,
            IsAreaSkill = false,
            ShotCount = 1,
            DamageMultiplier = 1f,
            DamageReductionRate = 0f,
            Duration = 0f,
            HealAmount = 0f
        };
    }
    public string SkillId => _skillId;
    public SkillType SkillType => _skillType;
    public float InitCooldown => _initCooldown;
    public float Cooldown => _cooldown;
    public float SkillRange => _skillRange;
    public GameObject EffectPrefab => _effectPrefab;
}
