using UnityEngine;

[CreateAssetMenu(fileName = "SupportSkillSO", menuName = "Scriptable Objects/SupportSkillSO")]
public class AngelSkillSO : SkillDataSO
{
    [Header("지원형 설정")]
    [SerializeField] private float _radius;
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

        runtime.Radius = _radius;
        runtime.HealAmount = _healAmount;
        runtime.Duration = _duration;
        runtime.SkillRange = SkillRange;
        runtime.IsAreaSkill = _isAreaSkill;

        if (_isAreaSkill)
        {
            runtime.Radius = _radius; 
        }
        else
        {
            runtime.Radius = 0f;  
        }

        return runtime;
    }
    public float HealAmount => _healAmount;
    public float Duration => _duration;
}
