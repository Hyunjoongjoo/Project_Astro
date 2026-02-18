using Fusion;
using System.Collections.Generic;
using UnityEngine;


public class Tower : Structure, IBasicAttack, ITargetFinder
{
    //26-02-13 주현중 수정 (임의로 범위,공격력 등등을 설정해서 진행)
    public static List<Structure> AliveTowers = new List<Structure>();

    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [SerializeField] private float _detectRange;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _scanInterval = 0.5f;

    private UnitBase _currentTarget;
    private float _nextScanTime;
    private float _nextAttackTime;

    public float AttackPower { get => _attackPower; }
    public float AttackSpeed { get => _attackSpeed; }
    public float AttackRange { get => _detectRange; }
    public float SearchRange { get => _detectRange; }
    public LayerMask TargetLayer { get => _targetLayer; }
    public float SearchInterval { get => _scanInterval; }

    [Networked] public TickTimer AttackInterval { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        if (team == Team.Blue)
        {
            gameObject.layer = LayerMask.NameToLayer("BlueTeam");
            _targetLayer = 1 << LayerMask.NameToLayer("RedTeam");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("RedTeam");
            _targetLayer = 1 << LayerMask.NameToLayer("BlueTeam");
        }
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
        if (AttackInterval.ExpiredOrNotRunning(Runner))
        {

        }
        TickTimer.CreateFromSeconds(Runner, 1.5f);
    }

    // 네트워크라 이거 쓰면 안됨
    private void Update()
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        UnitBase target = FindTarget();
        if (target == null)
        {
            return;
        }

        FireProjectile(target);

        _nextAttackTime = Time.time + _attackSpeed;
    }

    private void FireProjectile(UnitBase target)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        GameObject projectile = Instantiate(
            _projectilePrefab,
            _firePoint.position,
            Quaternion.identity
        );

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Fire(target.transform, _attackPower);
        }
    }

    public UnitBase FindTarget()//가까운 적 거리 기준 찾기
    {
        if (Time.time < _nextScanTime)
        {
            if (_currentTarget == null)
            {
                return null;
            }

            return _currentTarget;
        }

        _nextScanTime = Time.time + _scanInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);

        if (hits.Length == 0)
        {
            _currentTarget = null;
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

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = unit;
            }
        }
        _currentTarget = closest;
        return _currentTarget;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
#endif
}
