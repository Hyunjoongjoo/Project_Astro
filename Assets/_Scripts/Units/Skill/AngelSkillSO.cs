using UnityEngine;

[CreateAssetMenu(fileName = "SupportSkillSO", menuName = "Scriptable Objects/SupportSkillSO")]
public class AngelSkillSO : SkillDataSO
{
    [Header("지원형 설정")]
    [SerializeField] private float _healAmount;
    [SerializeField] private float _duration;
    [SerializeField] private bool _isAreaSkill;

    public override IHeroSkill CreateSkillComponent(GameObject owner)
    {
        return owner.AddComponent<AngelSkill>();
    }

    public override SkillRuntimeData CreateRuntimeData()
    {
        SkillRuntimeData runtime = base.CreateRuntimeData();

        runtime.HealAmount = _healAmount;
        runtime.Duration = _duration;
        runtime.IsAreaSkill = _isAreaSkill;

        return runtime;
    }
    public float HealAmount => _healAmount;
    public float Duration => _duration;
}
