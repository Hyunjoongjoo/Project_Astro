using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class UnitController : UnitBase
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
    [SerializeField] protected string unitId;
    public Transform firePoint;

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
    public string HeroId => unitId;
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

        UnitData data = TableManager.Instance.UnitTable.Get(unitId);

        //UnitStat 초기화
        _unitStat.Init(data);

        //Stat 기반 값 적용
        MaxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = MaxHealth;
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
        // 함교가 없다면 게임이 끝난 상태니 행동 중지
        if (_bridge == null)
            return null;

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

    // === RPC 메서드들 모음 ===

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayChildSkillEffect(
        NetworkId casterId,
        NetworkId targetId,
        SkillType type,
        bool setChild,
        float duration = 0f
        )
    {
        Debug.Log("스킬 이펙트 RPC 수신");

        // caster 찾기
        if (!Runner.TryFindObject(casterId, out NetworkObject casterObj))
        {
            return;
        }

        UnitController unit = casterObj.GetComponent<UnitController>();
        HeroController hero = unit as HeroController;

        GameObject prefab = null;

        // 평타 공격인가 스킬인가
        if (type == SkillType.normal_attack)
        {
            prefab = unit._normalAttackData.skillVFX;
        }
        else if (hero != null)
        {
            prefab = hero.GetSkillVFX(type);
        }

        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPos = unit.transform.position;
        Transform parent = null;

        // targetId가 존재하면 타겟 기준으로 처리
        if (Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            spawnPos = targetObj.transform.position;

            if (setChild)
            {
                parent = targetObj.transform;
            }
        }

        GameObject obj;

        if (parent != null)
        {
            obj = Instantiate(prefab, parent);
            obj.transform.localPosition = Vector3.zero;
        }
        else
        {
            obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        Destroy(obj, duration);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FireProjectile(
        NetworkId id,
        SkillType type,
        Vector3 targetPos,
        float power
        )
    {

        if (Runner.TryFindObject(id, out NetworkObject casterObj))
        {
            UnitController unit = casterObj.GetComponent<UnitController>();
            GameObject prefab = null;
            // 투사체형 이니까 투사체 SO가 반드시 있어야 함.
            ProjectileSkillSO projectileSO = null;

            // 평타 공격인가 스킬인가
            if (type == SkillType.normal_attack)
                projectileSO = unit._normalAttackData as ProjectileSkillSO;

            else
            {
                if (unit is HeroController)
                    projectileSO = ((unit as HeroController).CurUniqueSkill.Data) as ProjectileSkillSO;
            }

            if (projectileSO == null)
                return;

            prefab = projectileSO.skillVFX;

            int oneShotProjectileNum = projectileSO.oneShotProjectileNum;
            float spreadAngle = projectileSO.spreadAngle;
            int projectileCount = oneShotProjectileNum > 0 ? oneShotProjectileNum : 1;

            // 방사각 계산 (탄환이 2개 이상일 때만 각도 분할)
            float startAngle = projectileCount > 1 ? -spreadAngle / 2f : 0f;
            float angleStep = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;

            // 타겟을 향하는 기본 방향
            Vector3 directionToTarget = (targetPos - firePoint.position).normalized;
            Quaternion baseRotation = Quaternion.LookRotation(directionToTarget);

            for (int i = 0; i < projectileCount; i++)
            {
                // Y축(좌우) 기준으로 각도를 더해 최종 회전값 계산
                Quaternion spreadRotation = baseRotation * Quaternion.Euler(0, startAngle + (angleStep * i), 0);

                GameObject projectileObj = Instantiate(prefab, firePoint.position, spreadRotation);
                Projectile projectile = projectileObj.GetComponent<Projectile>();

                projectile.Initialize(projectileSO, networkedTeam, power, Runner);
                projectile.Fire(targetPos);
            }
        }
    }
}
