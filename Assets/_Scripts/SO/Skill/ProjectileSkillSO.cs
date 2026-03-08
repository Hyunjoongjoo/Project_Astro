using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkillSO", menuName = "Scriptable Objects/Skills/Projectile Skill")]
public class ProjectileSkillSO : BaseSkillSO
{
    [Header("투사체형 스킬의 속성")]
    public float damageRatio; // 데미지 계수
    public float range; // 사거리
    public float projectileSpeed; // 탄속
    public bool isHoming; // 유도성이 있는가
    public int oneShotProjectileNum; // 한 번 쏠 때 동시에 나가는 탄환 수
    public float spreadAngle; // 탄이 퍼지는 각도, 0이면 직선
    public int repeatingFire; // 스킬 시전 한번에 연발 횟수
    public float interval; // 연속 발사 시 간격
    public GameObject projectileVFX; // 투사체 비주얼

    // 만약 한 번에 3발씩 나가고 연발 수가 3이라면
    // 스킬 시전 한 번에 3발씩 3번, 총 9개의 투사체를 발사한다.

    public override ISkill CreateInstance(MinionController unit)
    {
        return new ProjectileSkill(this, unit);
    }
}
