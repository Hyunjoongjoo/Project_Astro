using UnityEngine;

[CreateAssetMenu(fileName = "HealSkillSO", menuName = "Scriptable Objects/Skills/Heal Skill")]
public class HealSkillSO : BaseSkillSO
{
    [Header("치유형 스킬의 속성")]
    public float healCoefficient = 1.5f;
    public float range;
    public float duration;
    public float interval;
    public float cooldownMultiplier = 1f;//쿨다운비율 30퍼쿨감->0.7

    [Header("광역화")]
    public bool areaOfEffect;

    public override ISkill CreateInstance(UnitController unit)
    {
        return new HealSkill(this, unit);
    }
}
