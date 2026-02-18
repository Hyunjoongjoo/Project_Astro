using UnityEngine;

public class HeroController : MobilityUnit, IBasicAttack
{
    [Header("공격 관련 스테이터스")]
    [SerializeField] protected float _attackDamage = 10f;
    [SerializeField] protected float _attackRange = 1.5f;
    [SerializeField] protected float _attackCooldown = 1f;

    [Header("공격 타입")]
    [SerializeField] private AttackType _attackType;

    [Header("원거리")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    public float AttackPower => _attackDamage;
    public float AttackSpeed => _attackCooldown;
    public float AttackRange => _attackRange;

    public void BaseAttack(UnitBase target)
    {
        if (target == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance > AttackRange)
        {
            return;
        }

        switch (_attackType)
        {
            case AttackType.Melee:
                AttackMelee(target);
                break;

            case AttackType.Range:
                AttackRanged(target);
                break;
        }
    }

    private void AttackMelee(UnitBase target)
    {
        target.TakeDamage(AttackPower);
    }

    private void AttackRanged(UnitBase target)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        GameObject projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);

        projectile.GetComponent<Projectile>().Fire(target.transform, AttackPower);
    }
}
