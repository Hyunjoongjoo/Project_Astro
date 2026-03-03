using UnityEngine;

[CreateAssetMenu(fileName = "DefenseSkillSO", menuName = "Scriptable Objects/DefenseSkillSO")]
public class DefenseSkillSO : SkillDataSO
{
    [Header("방어형 설정")]
    [SerializeField] private float _duration;
    [SerializeField] private float _damageReductionRate;

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.Duration = Duration;
        runtime.DamageMultiplier = DamageReductionRate;

        return runtime;
    }
    public float Duration => _duration;
    public float DamageReductionRate => _damageReductionRate;
}
