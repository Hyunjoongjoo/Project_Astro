using UnityEngine;

[CreateAssetMenu(fileName = "DefenseSkillSO", menuName = "Scriptable Objects/DefenseSkillSO")]
public class TankSkillSO : SkillDataSO
{
    [Header("방어형 설정")]
    [SerializeField] private float _duration;
    [SerializeField] private float _damageReductionRate;

    public override IHeroSkill CreateSkillComponent(GameObject owner)
    {
        return owner.AddComponent<TankSkill>();
    }

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.Duration = Duration;
        runtime.DamageReductionRate = DamageReductionRate;

        return runtime;
    }
    public float Duration => _duration;
    public float DamageReductionRate => _damageReductionRate;
}
