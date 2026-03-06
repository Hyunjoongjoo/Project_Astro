using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class NewHeroController : UnitBase
{
    [Header("내비게이션 및 탐지")]
    public NavMeshAgent agent;
    public float moveSpeed;
    public float searchRange;
    public LayerMask targetLayer;
    public float searchInterval = 0.3f;

    [Header("전투 관련")]
    public float attackRange;
    public UnitBase currentTarget; // 현재 타겟

    // 상태 기계 및 상태 인스턴스들
    public StateMachine StateMachine { get; private set; }
    public DeployState DeployState { get; private set; }
    public DetectState DetectState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }
    public DieState DieState { get; private set; }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            // 상태 인스턴스 생성
            StateMachine = new StateMachine();
            DeployState = new DeployState(this);
            DetectState = new DetectState(this);
            ChaseState = new ChaseState(this);
            AttackState = new AttackState(this);
            DieState = new DieState(this);

            StateMachine.ChangeState(DeployState);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return; // 사망 시 중단 (혹은 DieState에서 처리)

        StateMachine.Update();
    }

    // --- 유틸리티 메서드 (상태 클래스들에서 호출해서 사용) ---

    public void BeginDeploy(Vector3 targetPos, float deployDelay)
    {
        if (!Object.HasStateAuthority) return;

        DeployState.SetDeployData(targetPos, deployDelay);
        StateMachine.ChangeState(DeployState);
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    public void StopMove()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public UnitBase FindTarget()
    {
        // 기존 MobilityUnit의 OverlapSphere 탐색 로직
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRange, targetLayer);
        float minDistance = float.MaxValue;
        UnitBase closest = null;

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit != null && unit != this && !unit.IsDead && unit.team != this.team)
            {
                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = unit;
                }
            }
        }
        return closest;
    }
}
