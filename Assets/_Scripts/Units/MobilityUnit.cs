using Fusion;
using UnityEngine;
using UnityEngine.AI;

// 이동을 하는 유닛의 부모 클래스
public class MobilityUnit : UnitBase, ITargetFinder
{
    [Header("내비게이션")]
    [SerializeField] protected NavMeshAgent agent;

    [Header("이동 관련 스테이터스")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected MoveType moveType;

    [Header("탐지 관련 스테이터스")]
    [SerializeField] protected float searchRange;
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected float searchInterval = 0.3f;

    public float SearchRange => searchRange;
    public LayerMask TargetLayer => targetLayer;
    public float SearchInterval => searchInterval;

    //팀과 공격타겟 설정등 셋업
    public virtual void Setup()
    {
        agent.speed = moveSpeed;

        ConfigureAreaMask();

        int myLayer;
        int enemyLayer;

        if (team == Team.Blue)
        {
            myLayer = LayerMask.NameToLayer("BlueTeam");
            enemyLayer = LayerMask.NameToLayer("RedTeam");
        }
        else if (team == Team.Red)
        {
            myLayer = LayerMask.NameToLayer("RedTeam");
            enemyLayer = LayerMask.NameToLayer("BlueTeam");
        }
        else
        {
            Debug.Log("중립 오브젝트입니다.");
            return;
        }

        //팀레이어 적용
        SetLayer(gameObject, myLayer);
        //탐지 단계에서는 적 팀 레이어만 대상으로
        targetLayer = 1 << enemyLayer;
    }

    private void SetLayer(GameObject root, int layer)//UnitBase가 붙은 오브젝트만 대상으로 레이어를 설정
    {
        if (root.GetComponent<UnitBase>() != null)
        {
            root.layer = layer;
        }

        foreach (Transform child in root.transform)
        {
            SetLayer(child.gameObject, layer);
        }
    }

    private void ConfigureAreaMask()
    {
        if (agent == null)
        {
            return;
        }

        int meteorArea = NavMesh.GetAreaFromName("MeteorZone");

        switch (moveType)
        {
            case MoveType.Small:
                //Small은 통과
                agent.areaMask = NavMesh.AllAreas;
                break;

            case MoveType.Large:
                //MeteorZone 차단
                agent.areaMask = NavMesh.AllAreas & ~(1 << meteorArea);
                break;
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
        agent.velocity = Vector3.zero;
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

            if (unit.IsDead)
            {
                continue;
            }
            
            //탐지 단계에서는 대략적인 거리 비교
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
        Gizmos.DrawWireSphere(transform.position, searchRange);
    }
#endif
}
