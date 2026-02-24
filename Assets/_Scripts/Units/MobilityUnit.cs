using Fusion;
using UnityEngine;
using UnityEngine.AI;

// 이동을 하는 유닛의 부모 클래스
public class MobilityUnit : UnitBase, ITargetFinder
{
    [Header("내비게이션")]
    [SerializeField] protected NavMeshAgent agent;

    [Header("이동 관련 스테이터스")]
    [SerializeField] protected float _moveSpeed;

    [Header("탐지 관련 스테이터스")]
    [SerializeField] protected float _searchRange;
    [SerializeField] protected LayerMask _targetLayer;
    [SerializeField] protected float _searchInterval = 0.3f;

    public float MoveSpeed => _moveSpeed;
    public float SearchRange => _searchRange;
    public LayerMask TargetLayer => _targetLayer;
    public float SearchInterval => _searchInterval;

    //팀과 공격타겟 설정등 셋업
    public virtual void Setup()
    {
        agent.speed = _moveSpeed;

        if (team == Team.Blue)
        {
            gameObject.layer = LayerMask.NameToLayer("BlueTeam");
            _targetLayer = 1 << LayerMask.NameToLayer("RedTeam");
        }
        else if (team == Team.Red)
        {
            gameObject.layer = LayerMask.NameToLayer("RedTeam");
            _targetLayer = 1 << LayerMask.NameToLayer("BlueTeam");
        }
        else
        {
            Debug.Log("중립 오브젝트입니다.");
        }
    }

    protected virtual void MoveTo(Vector3 destination)
    {
        //현재는 네비메쉬 기반, 추후 변경될수도 있음
        if (agent == null || !agent.enabled)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    protected virtual void StopMove()
    {
        if (agent == null || !agent.enabled)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }


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

            if (unit == this)
            {
                continue;
            }

            if (unit.team == team)
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

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _searchRange);
    }
#endif
}
