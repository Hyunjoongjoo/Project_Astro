using UnityEngine;

[CreateAssetMenu(fileName = "HealSkillSO", menuName = "Scriptable Objects/Skills/Heal Skill")]
public class HealSkillSO : BaseSkillSO
{
    [Header("치유형 스킬의 속성")]
    public float recoveryAmount;
    public float range;
    public float duration;
    public float interval;
    [Header("광역화")]
    public bool areaOfEffect;

    public override ISkill CreateInstance(UnitController unit)
    {
        return new HealSkill(this, unit);
    }
}
