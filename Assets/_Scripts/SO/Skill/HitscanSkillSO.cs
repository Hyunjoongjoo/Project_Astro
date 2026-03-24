using UnityEngine;

[CreateAssetMenu(fileName = "HitscanSkillSO", menuName = "Scriptable Objects/Skills/Hitscan Skill")]
public class HitscanSkillSO : BaseSkillSO
{
    [Header("히트스캔 스킬 속성")]
    public float damageRatio = 1f;
    public float range = 4f;

    public override ISkill CreateInstance(UnitController unit)
    {
        return new HitscanSkill(this, unit);
    }
}
