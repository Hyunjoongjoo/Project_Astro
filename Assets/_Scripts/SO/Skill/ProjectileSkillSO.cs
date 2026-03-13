using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkillSO", menuName = "Scriptable Objects/Skills/Projectile Skill")]
public class ProjectileSkillSO : BaseSkillSO
{
    [Header("투사체형 스킬의 속성")]
    public float damageRatio = 1f; // 데미지 계수
    public float range; // 사거리
    public float projectileSpeed; // 탄속
    public bool isHoming; // 유도성이 있는가
    public int oneShotProjectileNum; // 한 번 쏠 때 동시에 나가는 탄환 수
    public float spreadAngle; // 탄이 퍼지는 각도, 0이면 직선
    public int repeatingFire; // 스킬 시전 한번에 연발 횟수
    public float interval; // 연속 발사 시 간격
    public int pierceCount = 0; // 관통 횟수
    public int attackCount; //N번 공격 후 발동

    [Header("광역화")]
    public bool areaOfEffect;
    public float attackRange; // 광역 공격 시 범위

    // 만약 한 번에 3발씩 나가고 연발 수가 3이라면
    // 스킬 시전 한 번에 3발씩 3번, 총 9개의 투사체를 발사한다.

    public override ISkill CreateInstance(UnitController unit)
    {
        return new ProjectileSkill(this, unit);
    }
}
