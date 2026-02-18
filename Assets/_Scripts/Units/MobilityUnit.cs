using Fusion;
using UnityEngine;

// 이동을 하는 유닛의 부모 클래스
public class MobilityUnit : UnitBase, ITargetFinder
{
    [Header("이동 관련 스테이터스")]
    [SerializeField] protected float _moveSpeed;

    [Header("탐지 관련 스테이터스")]
    [SerializeField] protected float _searchRange;
    [SerializeField] protected LayerMask _targetLayer;
    [SerializeField] protected float _searchInterval;

    public float MoveSpeed => _moveSpeed;

    public float SearchRange => _searchRange;

    public LayerMask TargetLayer => _targetLayer;

    public float SearchInterval => _searchInterval;

    public UnitBase FindTarget()
    {
        // 네트워크 땐 TickTimer 사용하여 주기적으로 스캔

        Collider[] hits = Physics.OverlapSphere(transform.position, SearchRange, TargetLayer);

        float minDistance = float.MaxValue;
        UnitBase closest = null;

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = unit;
            }
        }

        return closest;
    }
}
