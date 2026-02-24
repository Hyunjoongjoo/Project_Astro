using System;
using Fusion;
using UnityEngine;

public class Bridge : Structure, IBasicAttack, ITargetFinder
{
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
    private UnitFSM _fsm;

    public float AttackPower { get => _attackPower; }
    public float AttackSpeed { get => _attackSpeed; }
    public float AttackRange { get => _detectRange; }
    public float SearchRange { get => _detectRange; }
    public LayerMask TargetLayer { get => _targetLayer; }
    public float SearchInterval { get => _scanInterval; }

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
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
#endif
}
