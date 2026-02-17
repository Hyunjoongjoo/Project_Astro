using Fusion;
using UnityEngine;
using UnityEngine.AI;

//일단 임의로 상태 분리해둠
public enum AutoBattleState { Advance, Combat }

//공격 대상 우선순위 enum
public enum AttackObjective { Unit, Tower, Main }

/// <summary>
/// 자동 전투 공통 AI로직 담당
/// 상태 전환,탐지,이동, 유효성 검사
/// 상태별 행동은 파생 클래스에서 구현
/// </summary>
public abstract class BaseAutoBattleAI : NetworkBehaviour
{
    [Header("네비")]
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Transform finalGoal;

    [Header("탐지")]
    [SerializeField] protected float detectRadius = 10f;//탐지 범위
    [SerializeField] protected LayerMask targetLayer;//탐지 대상
    [SerializeField] protected float scanInterval = 0.5f;//스캔간격0.5초

    [Header("팀")]
    [SerializeField] protected Team team;

    protected AutoBattleState currentState;
    protected Transform currentTarget; //유닛전투용
    protected Transform currentObjective; //이동목표
    protected AttackObjective currentObjectiveType;

    protected UnitController controller;

    private float _nextScanTime;

    public Team Team => team;

    protected virtual void Awake()
    {
        controller = GetComponent<UnitController>();
        //기본 상태는 전진
        currentState = AutoBattleState.Advance;
        _nextScanTime = Time.time + Random.Range(0f, scanInterval);
    }

    //public override void Spawned()//Spawn 완료됐을 때 호출되는 Fusion 콜백, 네트워크 연결전이라 주석처리
    //{
    //    //AI 판단은 State Authority를 가진 클라이언트만 수행(PUN의 마스터클라이언트와 유사)
    //    if (!Object.HasStateAuthority)
    //    {
    //        return;
    //    }

    //    //첫 스캔 시점을 동일하지 않게(순간적인 프레임드랍 방지)
    //    float now = (float)Runner.SimulationTime;
    //    _nextScanTime = now + Random.Range(0f, scanInterval);
    //}

    //public override void FixedUpdateNetwork()
    //{
    //    //if (!Object.HasStateAuthority)
    //    //{
    //    //    return;
    //    //}

    //    UpdateState();
    //}

    private void Update()//네트워크 적용전 업데이트로 테스트
    {
        if (Runner == null)
        {
            UpdateState();
        }
    }

    protected virtual void UpdateState()//상태 업데이트
    {
        switch (currentState)
        {
            case AutoBattleState.Advance:
                UpdateAdvance();
                break;

            case AutoBattleState.Combat:
                UpdateCombat();
                break;

        }
    }

    protected void ChangeState(AutoBattleState nextState)//상태 전환 메서드
    {
        if (currentState == nextState)
        {
            return;
        }

        StopMove();
        currentState = nextState;
    }

    protected virtual void UpdateAdvance()
    {
        UpdateObjective();

        if (currentObjective != null)
        {
            float distance = Vector3.Distance(transform.position, currentObjective.position);

            if (currentObjectiveType == AttackObjective.Tower && distance <= controller.AttackRange)
            {
                StopMove();
                controller.Attack(currentObjective);
                return;
            }

            MoveTo(currentObjective.position);
        }

        if (FindTarget())
        {
            ChangeState(AutoBattleState.Combat);
        }
    }

    protected abstract void UpdateCombat();//파생 클래스에서 구현

    protected bool FindTarget()//가까운 적 거리 기준 찾기
    {
        //float now = (float)Runner.SimulationTime; //네트워크 연결전이라 주석처리
        //if (now < _nextScanTime)
        //{
        //    return currentTarget != null;
        //}

        //_nextScanTime = now + scanInterval;

        if (Time.time < _nextScanTime)
        {
            return currentTarget != null;
        }

        _nextScanTime = Time.time + scanInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, targetLayer);

        float minDistance = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = hit.transform;
            }
        }

        currentTarget = closest;
        return currentTarget != null;
    }

    protected void UpdateObjective()
    {
        //살아있는 가까운 적타워 찾기
        Transform tower = FindNearestTower();
        if (tower != null)
        {
            currentObjective = tower;
            currentObjectiveType = AttackObjective.Tower;
            return;
        }

        currentObjective = finalGoal;
        currentObjectiveType = AttackObjective.Main;
    }

    protected Transform FindNearestTower()//가까운 타워 찾기
    {
        float minDistance = float.MaxValue;
        Structure closest = null;

        foreach (var tower in Tower.AliveTowers)
        {
            if (tower == null)
            {
                continue;
            }

            if (tower.team == team)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = tower;
            }
        }

        if (closest != null)
        {
            return closest.transform;
        }
        else
        {
            return null;
        }
    }

    protected bool IsTargetValid()//타겟 확인용 메서드
    {
        if (currentTarget == null)
        {
            return false;
        }

        return currentTarget.gameObject.activeInHierarchy;
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

    //팀과 공격타겟 설정등 셋업
    public virtual void Setup(Team team, Transform targetBase)
    {
        this.team = team;

        if (team == Team.Blue)
        {
            gameObject.layer = LayerMask.NameToLayer("BlueTeam");
            targetLayer = 1 << LayerMask.NameToLayer("RedTeam");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("RedTeam");
            targetLayer = 1 << LayerMask.NameToLayer("BlueTeam");
        }

        if (targetBase != null)
        {
            finalGoal = targetBase;
        }

        ChangeState(AutoBattleState.Advance);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()//탐지 범위 시각화
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
#endif
}
