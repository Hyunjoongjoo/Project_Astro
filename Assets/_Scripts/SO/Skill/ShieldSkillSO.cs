using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSkillSO", menuName = "Scriptable Objects/Skills/Shield Skill")]
public class ShieldSkillSO : BaseSkillSO
{
    [Header("실드형 스킬의 속성")]
    public float damageReduction;
    public float duration;

    [Header("광역화")]
    public float aoeDamageRatio;   // N초마다 주는 피해 비율
    public float aoeRange;         // 광역 범위
    public float aoeInterval = 1f; // 피해 주기

    public override ISkill CreateInstance(UnitController unit)
    {
        return new ShieldSkill(this, unit);
    }
}
