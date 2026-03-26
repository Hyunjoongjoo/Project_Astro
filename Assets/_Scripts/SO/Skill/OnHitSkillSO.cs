using UnityEngine;

[CreateAssetMenu(fileName = "OnHitSkillSO", menuName = "Scriptable Objects/Skills/OnHit Skill")]
public class OnHitSkillSO : BaseSkillSO
{
    [Header("평타 강화형 스킬의 속성")]
    public float additionalDamageRatio; // 추가 데미지 계수
    public float additionalRange; // 추가 사거리
    public int onhitPerAttack; // 몇 타마다 발동하는가?

    public override ISkill CreateInstance(UnitController unit)
    {
        return new OnHitSkill(this, unit);
    }
}
