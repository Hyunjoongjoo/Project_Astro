using System.Collections.Generic;
using Fusion;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class Tower : Structure, IBasicAttack, ITargetFinder
{
    public static List<Structure> AliveTowers = new List<Structure>();

    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [SerializeField] private float _detectRange;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _scanInterval = 0.5f;

    private UnitBase _currentTarget;

    private TickTimer _scanTimer;
    private TickTimer _attackTimer;

    public float AttackPower { get => _attackPower; }
    public float AttackSpeed { get => _attackSpeed; }
    public float AttackRange { get => _detectRange; }
    public float SearchRange { get => _detectRange; }
    public LayerMask TargetLayer { get => _targetLayer; }
    public float SearchInterval { get => _scanInterval; }

    //[Networked] public TickTimer AttackInterval { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority)
        {
            return;
        }

        CurrentState = UnitState.Idle;

        _scanTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        //if (team == Team.Blue)
        //{
        //    gameObject.layer = LayerMask.NameToLayer("BlueTeam");
        //    _targetLayer = 1 << LayerMask.NameToLayer("RedTeam");
        //}
        //else
        //{
        //    gameObject.layer = LayerMask.NameToLayer("RedTeam");
        //    _targetLayer = 1 << LayerMask.NameToLayer("BlueTeam");
        //}
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

        UpdateDetect();
        UpdateAttack();
    }

    private void UpdateDetect()
    {
        if (!_scanTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        _currentTarget = FindTarget();
        _scanTimer = TickTimer.CreateFromSeconds(Runner, _scanInterval);
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
        _currentTarget.TakeDamage(_attackPower);

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackEffect(Vector3 hitPos)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        var proj = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
        proj.GetComponent<Projectile>()?.Fire(hitPos);
    }

    public UnitBase FindTarget()//가까운 적 거리 기준 찾기
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);

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

            //if (unit.team == this.team)//26-02-20 현재 팀설정에 문제가 있는듯함
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
#endif
}
