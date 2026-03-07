using System.Collections.Generic;
using Fusion;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class Tower : Structure, IBasicAttack, ITargetFinder
{
    public static List<Structure> AliveTowers = new List<Structure>();

    [Header("타워 스테이터스")]
    [SerializeField] private string _unitId;
    [SerializeField] private UnitStat _unitStat;
    [SerializeField] private float _attackRange;

    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _scanInterval = 0.5f;

    private UnitBase _currentTarget;

    private TickTimer _scanTimer;
    private TickTimer _attackTimer;
    private UnitFSM _fsm;

    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float SearchRange => _unitStat.DetectRange.Value;
    public float AttackRange => _attackRange;
    public LayerMask TargetLayer => _targetLayer;
    public float SearchInterval => _scanInterval;


    public override void Spawned()
    {
        base.Spawned();

        unitType = UnitType.Tower;

        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (_unitStat == null)
        {
            _unitStat = GetComponent<UnitStat>();
        }

        UnitData data = TableManager.Instance.UnitTable.Get(_unitId);

        _unitStat.Init(data);

        maxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = maxHealth;

        Debug.Log(
    $"[적용된 타워 스텟]\n" +
    $"ID : {_unitId}\n" +
    $"HP : {maxHealth}\n" +
    $"Attack : {AttackPower}\n" +
    $"AttackSpeed : {AttackSpeed}\n" +
    $"DetectRange : {SearchRange}"
    );

        _fsm = new UnitFSM();

        CurrentState = UnitState.Idle;

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
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (CurrentState == UnitState.Dead)
        {
            return;
        }

        if (_fsm.State == UnitAIState.Detect && _scanTimer.ExpiredOrNotRunning(Runner))
        {
            _currentTarget = FindTarget();
            _scanTimer = TickTimer.CreateFromSeconds(Runner, _scanInterval);
        }

        bool hasTarget = _currentTarget != null;
        bool inRange = hasTarget && Vector3.Distance(transform.position, _currentTarget.transform.position) <= AttackRange;
        bool isDead = CurrentState == UnitState.Dead;

        _fsm.DecideState(isDead, hasTarget, inRange);
        ApplyState(_fsm.State);
    }

    private void ApplyState(UnitAIState state)
    {
        switch (state)
        {
            case UnitAIState.Detect:
                HandleDetect();
                break;

            case UnitAIState.Attack:
                HandleAttack();
                break;

            case UnitAIState.Dead:
                break;
        }
    }

    //private void UpdateDetect()
    //{
    //    if (!_scanTimer.ExpiredOrNotRunning(Runner))
    //    {
    //        return;
    //    }

    //    _currentTarget = FindTarget();
    //    _scanTimer = TickTimer.CreateFromSeconds(Runner, _scanInterval);
    //}

    private void HandleDetect()
    {
        CurrentState = UnitState.Idle;
    }

    private void HandleAttack()
    {
        CurrentState = UnitState.Attack;
        UpdateAttack();
    }

    private void UpdateAttack()
    {
        if (_currentTarget == null)
        {
            return;
        }

        if (!_attackTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }
        _currentTarget.TakeDamage(AttackPower);

        RPC_PlayAttackEffect(_currentTarget.transform.position);

        float cooldown;

        if (AttackSpeed > 0f)
        {
            cooldown = 1f / AttackSpeed;
        }
        else
        {
            cooldown = 1f;
        }

        _attackTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
    }

    //투사체(현재는 이펙트만)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackEffect(Vector3 targetPos)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        var projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
        projectile.GetComponent<Projectile>()?.Fire(targetPos, team);
    }

    public UnitBase FindTarget()//가까운 적 거리 기준 찾기
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, SearchRange, _targetLayer);

        if (hits.Length == 0)
        {
            return null;
        }

        float minDistance = float.MaxValue;
        UnitBase closest = null;

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }

            //if (unit.team == this.team)
            //{
            //    continue;
            //}

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
        _fsm?.ForceDead();
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
