using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkillSO", menuName = "Scriptable Objects/Skills/Projectile Skill")]
public class ProjectileSkillSO : BaseSkillSO
{
    public float damageRatio; // 데미지 계수
    public float range; // 사거리
    public int oneShotProjectileNum; // 한 번 쏠 때 동시에 나가는 탄환 수
    public float spreadAngle; // 탄이 퍼지는 각도, 0이면 직선
    public int repeatingFire; // 스킬 시전 한번에 연발 횟수
    public float interval; // 연속 발사 시 간격
    public GameObject projectileVFX;

    // 만약 한 번에 3발씩 나가고 연발 수가 3이라면
    // 스킬 시전 한 번에 3발씩 3번, 총 9개의 투사체를 발사한다.

    public override ISkill CreateInstance()
    {
        return new ProjectileSkill(this);
    }
}
