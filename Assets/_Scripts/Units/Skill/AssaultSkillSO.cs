using UnityEngine;

[CreateAssetMenu(fileName = "AssaultSkillSO", menuName = "Scriptable Objects/AssaultSkillSO")]
public class AssaultSkillSO : SkillDataSO
{
    [Header("강습형 설정")]
    [SerializeField] private float _radius;
    [SerializeField] private float _damageMultiplier;
    [SerializeField] private float _effectLifeTime;

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.Radius = _radius;
        runtime.DamageMultiplier = _damageMultiplier;
        runtime.Duration = _effectLifeTime;
        runtime.IsAreaSkill = true;

        return runtime;
    }
    public float Radius => _radius;
    public float DamageMultiplier => _damageMultiplier;
    public float EffectLifeTime => _effectLifeTime;
}
