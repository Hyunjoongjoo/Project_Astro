using UnityEngine;

[CreateAssetMenu(fileName = "ChainSkillSO", menuName = "Scriptable Objects/Skills/Chain Skill")]
public class ChainSkillSO : BaseSkillSO
{
    [Header("최대 전이 횟수(첫 타격 제외)")]
    public int maxChainCount = 3; // 최대 전이 횟수

    [Header("스킬 사거리")]
    public float range = 5f;

    [Header("전이 범위")]
    public float chainRange = 3f; // 전이 범위

    [Header("데미지 비율(유닛 공격력*비율 첫 타격 : 1 = 100%)")]
    public float damageRatio = 1f; 

    [Header("전이 데미지 배율(0.6 = 첫 타격 100% 이후 60% 데미지)")]
    public float chainDamageMultiplier = 0.6f; // 1회 감소율 0.6f= 60%, 1타 100% 이후 전부 60%

    [Header("타겟 옵션(true = 영웅만, false = 전체 대상(영웅우선))")]
    public bool heroOnly = true;// true = 영웅만, false = 전체 대상(영웅우선)

    public override ISkill CreateInstance(UnitController unit)
    {
        return new ChainSkill(this, unit);
    }
}
