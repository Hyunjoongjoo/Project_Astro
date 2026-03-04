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
    [SerializeField] private string _heroId;
    [SerializeField] private SkillType _skillType;
    [SerializeField] private string _skillName;
    [SerializeField] private string _skillDescription;
    [SerializeField] private string _note;
    [SerializeField] private float _initCooldown;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _skillRange;
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private float _effectLifeTime;

    public virtual SkillRuntimeData CreateRuntimeData()
    {
        return new SkillRuntimeData
        {
            Cooldown = _cooldown,
            SkillRange = _skillRange,
            EffectPrefab = _effectPrefab,
            EffectLifeTime = _effectLifeTime,
            IsAreaSkill = false,
            ShotCount = 1,
            DamageMultiplier = 1f,
            DamageReductionRate = 0f,
            Duration = 0f,
            HealAmount = 0f
        };
    }
    public string SkillId => _skillId;
    public string HeroId => _heroId;
    public SkillType SkillType => _skillType;
    public string SkillName => _skillName;
    public string SkillDescription => _skillDescription;
    public string Note => _note;
    public float InitCooldown => _initCooldown;
    public float Cooldown => _cooldown;
    public float SkillRange => _skillRange;
    public GameObject EffectPrefab => _effectPrefab;
    public float EffectLifeTime => _effectLifeTime;
}
