using UnityEngine;

[CreateAssetMenu(fileName = "DashSkillSO", menuName = "Scriptable Objects/Skills/Dash Skill")]
public class DashSkillSO : BaseSkillSO
{
    [Header("돌진형 스킬의 속성")]
    public float dashSpeed;
    public float canDashMaxDistance; // 돌진 최대 거리
    public float canDashMinDistance; // 돌진 최소 거리
    public int dashCount = 1;        // 돌진 횟수

    [Header("목표점 도착 후 공격 속성")]
    public float damageRatio; // 돌진 대상에게 피해. 데미지 없다면 0

    [Header("광역화")]
    public bool areaOfEffect;
    public float attackRange; // 광역 공격 시 범위

    public override ISkill CreateInstance(UnitController unit)
    {
        return new DashSkill(this, unit);
    }
}
