using System;
using System.Collections.Generic;
using UnityEngine;


public class Tower : MonoBehaviour
{
    [SerializeField] private Team _team;
    [SerializeField] private float _hp;
    public event Action OnTowerDestroyed;
    private bool _isDestroyed = false;

    //26-02-13 주현중 수정 (임의로 범위,공격력 등등을 설정해서 진행)
    public static List<Tower> AliveTowers = new List<Tower>();

    [SerializeField] private float _damage;
    [SerializeField] private float _detectRange;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _scanInterval = 0.5f;
    [SerializeField] private float _attackInterval = 1f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    private UnitController _currentTarget;
    private float _nextScanTime;
    private float _nextAttackTime;

    public Team Team => _team;

    private void Awake()
    {
        if (_team == Team.Blue)
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

    private void Update()
    {
        if (_isDestroyed)
        {
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        UnitController target = FindTarget();
        if (target == null)
        {
            return;
        }

        FireProjectile(target);

        _nextAttackTime = Time.time + _attackInterval;
    }

    private void FireProjectile(UnitController target)
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
            proj.Fire(target.transform, _damage);
        }
    }

    private UnitController FindTarget()//가까운 적 거리 기준 찾기
    {
        if (Time.time < _nextScanTime)
        {
            if (_currentTarget == null)
            {
                return null;
            }

            if (_currentTarget.IsDead)
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
        UnitController closest = null;

        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            BaseAutoBattleAI ai = hit.GetComponent<BaseAutoBattleAI>();
            if (ai == null)
            {
                continue;
            }

            if (ai.Team == _team)
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

    public void TakeDamage(float amount)
    {
        if (_isDestroyed) return;

        _hp -= amount;
        if (_hp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (_isDestroyed)
        {
            return;
        }

        _isDestroyed = true;
        AliveTowers.Remove(this);
        OnTowerDestroyed?.Invoke();
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
#endif
}
