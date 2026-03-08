using UnityEngine;
using UnityEngine.AI;


public class MinionController : UnitBase
{
    [Header("내비게이션 및 탐지")]
    public NavMeshAgent agent;
    public float moveSpeed;
    public float searchRange;
    public LayerMask targetLayer;
    public float searchInterval = 0.3f;
    [SerializeField] protected MoveType moveType;

    [Header("스탯 관련")]
    public float attackRange = 5;
    [SerializeField] protected string _unitId;
    public Transform firePoint;
    protected UnitStat _unitStat;

    [HideInInspector] public UnitBase currentTarget;

    protected UnitBase _towerA;
    protected UnitBase _towerB;
    protected UnitBase _bridge;

    public ISkill normalAttack;

    // 상태 머신과 상태 인스턴스들
    public StateMachine StateMachine { get; protected set; }
    public DetectState DetectState { get; protected set; }
    public ChaseState ChaseState { get; protected set; }
    public AttackState AttackState { get; protected set; }
    public DieState DieState { get; protected set; }

    [Header("스킬 데이터")]
    [Header("평타 공격")]
    [SerializeField] protected BaseSkillSO _normalAttackData;

    public UnitBase CurrentTarget => currentTarget;
    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public UnitStat UnitStat => _unitStat;
    public NavMeshAgent Agent => agent;
    public LayerMask TargetLayer => targetLayer;
    public string HeroId => _unitId;
    public LayerMask AllyLayer
    {
        get
        {
            return team == Team.Blue ? LayerMask.GetMask("BlueTeam") : LayerMask.GetMask("RedTeam");
        }
    }
    public override void Spawned()
    {
        base.Spawned();

        unitType = UnitType.Minion;

        normalAttack = _normalAttackData.CreateInstance(this);

        if (!Object.HasStateAuthority) return;

        // 상태 인스턴스 생성
        StateMachine = new StateMachine();
        DetectState = new DetectState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        DieState = new DieState(this);

        _unitStat = GetComponent<UnitStat>();

        UnitData data = TableManager.Instance.UnitTable.Get(_unitId);

        //UnitStat 초기화
        _unitStat.Init(data);

        //Stat 기반 값 적용
        maxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = maxHealth;
        moveSpeed = _unitStat.MoveSpeed.Value;
        searchRange = _unitStat.DetectRange.Value;
        agent.speed = moveSpeed;

        StateMachine.ChangeState(DetectState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return; // 사망 시 중단 (혹은 DieState에서 처리)

        StateMachine.Update();
    }

    // --- 생성시 초기화 관련 메서드 ---

    // 스폰 전에 실행되는 메서드

    public void Setup(Team myTeam)
    {
        team = myTeam;
        agent = GetComponent<NavMeshAgent>();

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

        UnitBase[] targetStructure = team == Team.Blue ?
            ObjectContainer.Instance.redSideStructure :
            ObjectContainer.Instance.blueSideStructure;

        _towerA = targetStructure[0];
        _towerB = targetStructure[1];
        _bridge = targetStructure[2];
    }

    protected void SetLayer(GameObject root, int layer)//UnitBase가 붙은 오브젝트만 대상으로 레이어를 설정
    {
        if (root.GetComponent<UnitBase>() != null)
            root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayer(child.gameObject, layer);
    }

    protected void ConfigureAreaMask()
    {
        if (agent == null) return;

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

    // --- 유틸리티 메서드 (상태 클래스들에서 호출해서 사용) ---

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

    public UnitBase GetClosestTower()
    {
        //두 타워 다 없으면 함교
        if (_towerA == null && _towerB == null)
            return _bridge;

        //둘 중 하나만 남았다면 그 포탑으로
        if (_towerA == null)
            return _towerB;

        if (_towerB == null)
            return _towerA;

        float distA = (transform.position - _towerA.transform.position).sqrMagnitude;
        float distB = (transform.position - _towerB.transform.position).sqrMagnitude;

        return distA <= distB ? _towerA : _towerB;
    }

    public GameObject InstantiateObject(GameObject obj, Vector3 pos, Quaternion dir)
    {
        return Instantiate(obj, pos, dir);
    }
}
