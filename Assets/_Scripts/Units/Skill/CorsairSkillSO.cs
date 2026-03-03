using UnityEngine;

[CreateAssetMenu(fileName = "AssaultSkillSO", menuName = "Scriptable Objects/AssaultSkillSO")]
public class CorsairSkillSO : SkillDataSO
{
    [Header("강습형 설정")]
    [SerializeField] private float _radius;
    [SerializeField] private float _damageMultiplier;
    [SerializeField] private float _duration;

    public override IHeroSkill CreateSkillComponent(GameObject owner)
    {
        return owner.AddComponent<CorsairSkill>();
    }

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.Radius = _radius;
        runtime.DamageMultiplier = _damageMultiplier;
        runtime.Duration = _duration;
        runtime.IsAreaSkill = true;

        return runtime;
    }
    public float Radius => _radius;
    public float DamageMultiplier => _damageMultiplier;
    public float Duration => _duration;
}
