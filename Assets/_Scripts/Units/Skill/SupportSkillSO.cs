using UnityEngine;

[CreateAssetMenu(fileName = "SupportSkillSO", menuName = "Scriptable Objects/SupportSkillSO")]
public class SupportSkillSO : SkillDataSO
{
    [Header("지원형 설정")]
    [SerializeField] private float _healAmount;
    [SerializeField] private float _effectLifeTime;
    [SerializeField] private bool _isAreaSkill;

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.HealAmount = _healAmount;
        runtime.Duration = _effectLifeTime;
        runtime.IsAreaSkill = _isAreaSkill;

        return runtime;
    }
    public float HealAmount => _healAmount;
    public float EffectLifeTime => _effectLifeTime;
}
