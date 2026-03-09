using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class Tower : UnitBase
{
    public static List<Tower> AliveTowers = new List<Tower>();

    [Header("타워 스테이터스")]
    [SerializeField] private string _unitId;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _scanInterval = 0.5f;

    [Header("스킬 데이터")]
    [Header("타워 공격")]
    [SerializeField] protected BaseSkillSO _normalAttackData;

    private UnitBase _currentTarget;

    private TickTimer _scanTimer;
    private TickTimer _attackTimer;

    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float SearchRange => _unitStat.DetectRange.Value;
    public LayerMask TargetLayer => _targetLayer;
    public float SearchInterval => _scanInterval;


    public override void Spawned()
    {
        Debug.Log("Spawned 실행됨");
        base.Spawned();

        unitType = UnitType.Tower;

        if (!Object.HasStateAuthority) return;
        if (_unitStat == null) _unitStat = GetComponent<UnitStat>();

        UnitData data = TableManager.Instance.UnitTable.Get(_unitId);

        _unitStat.Init(data);

        MaxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = MaxHealth;

        Debug.Log(
            $"[적용된 타워 스텟]\n" +
            $"ID : {_unitId}\n" +
            $"HP : {MaxHealth}\n" +
            $"Attack : {AttackPower}\n" +
            $"AttackSpeed : {AttackSpeed}\n" +
            $"DetectRange : {SearchRange}"
            );

        _scanTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    private void OnEnable()
    {
        if (!AliveTowers.Contains(this))
        {
            AliveTowers.Add(this);
        }
    }

    private void OnDisable()
    {
        AliveTowers.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return;

        if (_currentTarget == null || _currentTarget.IsDead)
        {
            if (_scanTimer.ExpiredOrNotRunning(Runner))
            {
                _currentTarget = FindTarget();
                _scanTimer = TickTimer.CreateFromSeconds(Runner, _scanInterval);
            }
        }
        else
        {
            if (_attackTimer.ExpiredOrNotRunning(Runner))
            {
                PerformAttack();
                _attackTimer = TickTimer.CreateFromSeconds(Runner, _normalAttackData.cooldown);
            }
        }
    }

    private void PerformAttack()
    {
        _currentTarget.TakeDamage(AttackPower);
        RPC_PlayAttackEffect(_currentTarget.transform.position, AttackPower);
    }

    //투사체(현재는 이펙트만)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackEffect(Vector3 targetPos, float power)
    {
        if (_normalAttackData.skillVFX == null || _firePoint == null) return;

        // 타겟을 향하는 기본 방향
        Vector3 directionToTarget = (targetPos - _firePoint.position).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(directionToTarget);

        GameObject projectileObj = Instantiate(_normalAttackData.skillVFX, _firePoint.position, baseRotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        projectile.Initialize(_normalAttackData as ProjectileSkillSO, networkedTeam, power, Runner);
        projectile.Fire(targetPos);
    }

    public UnitBase FindTarget()//가까운 적 거리 기준 찾기
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, SearchRange, _targetLayer);

        if (hits.Length == 0) return null;

        float minDistance = float.MaxValue;
        UnitBase closest = null;

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = unit;
            }
        }
        return closest;
    }

    public override void Die()
    {
        base.Die();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, SearchRange);
    }
#endif
}
