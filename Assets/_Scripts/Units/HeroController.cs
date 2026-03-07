using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class HeroController : UnitBase
{
    [Header("내비게이션 및 탐지")]
    public NavMeshAgent agent;
    public float moveSpeed;
    public float searchRange;
    public LayerMask targetLayer;
    public float searchInterval = 0.3f;
    [SerializeField] protected MoveType moveType;

    [Header("전투 관련")]
    public float attackPower;
    public float attackRange;
    public float attackSpeed;
    [HideInInspector] public UnitBase currentTarget; // 현재 타겟

    [Header("스탯 관련")]
    [SerializeField] private string _heroId;
    [SerializeField] private float _attackRange;
    [SerializeField] private BaseSkillSO _skillData;
    [SerializeField] private BaseSkillSO _normalSkill;
    public Transform firePoint;
    private UnitStat _unitStat;

    [Header("스킬 데이터")]
    [Header("평타 공격")]
    [SerializeField] private BaseSkillSO _normalAttackData;
    [Header("기본 스킬 (증강 미적용)")]
    [SerializeField] private BaseSkillSO _standardSkillData;
    [Header("증강 A타입, 강화형")]
    [SerializeField] private BaseSkillSO _typeASkillData;
    [SerializeField] private BaseSkillSO _typeAEnhanceSkillData;
    [Header("증강 B타입, 강화형")]
    [SerializeField] private BaseSkillSO _typeBSkillData;
    [SerializeField] private BaseSkillSO _typeBEnhanceSkillData;


    private float _respawnTime;

    private UnitBase _enemyTowerA;
    private UnitBase _enemyTowerB;
    private UnitBase _enemyBridge;

    private GameObject _projectile;
    public ISkill normalAttack;
    public ISkill curUniqueSkill;
    private Vector3 _targetPos;
    private float _deployDelay;

    // 상태 머신과 상태 인스턴스들
    public StateMachine StateMachine { get; private set; }
    public DeployState DeployState { get; private set; }
    public DetectState DetectState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }
    public CastingState CastState { get; private set; }
    public DieState DieState { get; private set; }

    public float AttackRange => _attackRange;
    public UnitBase CurrentTarget => currentTarget;
    public float RespawnTime => _respawnTime;
    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float HealPower => _unitStat.HealPower.Value;
    public UnitStat UnitStat => _unitStat;
    public NavMeshAgent Agent => agent;
    public BaseSkillSO SkillData => _skillData;
    public GameObject Projectile => _projectile;
    public LayerMask TargetLayer => targetLayer;
    public string HeroId => _heroId;

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

        unitType = UnitType.Hero;

        normalAttack = _normalAttackData.CreateInstance(this);
        curUniqueSkill = _standardSkillData.CreateInstance(this);

        if (!Object.HasStateAuthority) return;
        
        // === 이 아래론 마스터 클라이언트가 아니면 실행되지 않음. ===
        // 상태 인스턴스 생성
        StateMachine = new StateMachine();
        DeployState = new DeployState(this);
        DetectState = new DetectState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        CastState = new CastingState(this);
        DieState = new DieState(this);

        _unitStat = GetComponent<UnitStat>();

        HeroStatData statData = HeroManager.Instance.GetStatus(_heroId);

        //UnitStat 초기화
        _unitStat.Init(statData);
        //Stat 기반 값 적용
        maxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = maxHealth;
        moveSpeed = _unitStat.MoveSpeed.Value;
        searchRange = _unitStat.DetectRange.Value;
        _respawnTime = _unitStat.RespawnTime.Value;
        agent.speed = moveSpeed;

        if (agent != null)
        {
            agent.enabled = false;

            // 스폰 위치가 네비 밖이거나 겹쳐있을 경우 보정
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }


            agent.enabled = true;
            agent.ResetPath();
        }
        DeployState.SetDeployData(_targetPos, _deployDelay);
        StateMachine.ChangeState(DeployState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return; // 사망 시 중단 (혹은 DieState에서 처리)

        // 기본 스킬의 시전은 어느 상태든 상관없이 조건만 만족하면 바로 전환한다.
        if (StateMachine.CurrentState != DeployState && StateMachine.CurrentState != CastState)
        {
            if (curUniqueSkill.UsingConditionCheck())
            {
                StateMachine.SavePreviousState();
                StateMachine.ChangeState(CastState);
                return;
            }
        }

        StateMachine.Update();
    }

    // --- 생성시 초기화 관련 메서드 ---

    // 스폰 전에 실행되는 메서드
    public void Setup(Team myTeam, Vector3 targetPos, float deployDelay)
    {
        team = myTeam;
        _targetPos = targetPos;
        _deployDelay = deployDelay;
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

        _enemyTowerA = targetStructure[0];
        _enemyTowerB = targetStructure[1];
        _enemyBridge = targetStructure[2];
    }

    private void SetLayer(GameObject root, int layer)//UnitBase가 붙은 오브젝트만 대상으로 레이어를 설정
    {
        if (root.GetComponent<UnitBase>() != null)
            root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayer(child.gameObject, layer);
    }

    private void ConfigureAreaMask()
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

    // 스킬 증강 시 스킬 교체
    public void ChangeSkill(BaseSkillSO newSkillData)
    {
        if (curUniqueSkill != null && newSkillData.GetType() == curUniqueSkill.GetType())
            curUniqueSkill.ChangeData(newSkillData);
        else
            curUniqueSkill = newSkillData.CreateInstance(this);
    }

    //스킬증강에 사용될 메서드
    private void ApplySkillAugments()
    {
        StageManager stageManager = FindFirstObjectByType<StageManager>();

        if (stageManager == null)
            return;

        if (!stageManager.PlayerDataMap.TryGet(Runner.LocalPlayer, out PlayerNetworkData data))
            return;

        //3.3 여현구
        //배열에서 구조체로 바뀌어서 여기 수정했습니다.
        for (int i = 0; i < SlotData_5.Length; i++)
        {
            string augmentId = data.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();

            if (string.IsNullOrEmpty(augmentId))
                continue;

            SkillAugmentSO so = AugmentController.Instance.GetSkillAugmentById(augmentId);
            if (so == null)
                continue;

            if (so.TargetHeroID != _heroId)
                continue;

            int tierIndex = data.TotalAugmentPicks >= 6 ? 1 : 0;

            if (tierIndex >= so.Tiers.Length)
                continue;

            SkillDataSO newSkill = so.Tiers[tierIndex].CombatSkillData;

            if (newSkill == null)
                continue;

            // EquipSkill(newSkill);
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
        if (_enemyTowerA == null && _enemyTowerB == null)
            return _enemyBridge;

        //둘 중 하나만 남았다면 그 포탑으로
        if (_enemyTowerA == null)
            return _enemyTowerB;

        if (_enemyTowerB == null)
            return _enemyTowerA;

        float distA = (transform.position - _enemyTowerA.transform.position).sqrMagnitude;
        float distB = (transform.position - _enemyTowerB.transform.position).sqrMagnitude;

        return distA <= distB ? _enemyTowerA : _enemyTowerB;
    }

    public void HealUnit(UnitBase target, float healAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        if (target == null || target.IsDead)
            return;

        target.CurrentHealth = Mathf.Min(
            target.CurrentHealth + healAmount,
            target.MaxHealth
        );
    }

    //포격형 전용 데미지 진입점
    public void ApplyBarrageSkillDamage(UnitBase target, float damageRatio)
    {
        if (!Object.HasStateAuthority)
            return;

        if (target == null || target.IsDead)
            return;

        float baseDamage = AttackPower;
        float finalDamage = baseDamage * damageRatio;

        target.TakeDamage(finalDamage);
    }

    public GameObject InstantiateObject(GameObject obj, Vector3 pos, Quaternion dir)
    {
        return Instantiate(obj, pos, dir);
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySkillEffect(Vector3 pos, Quaternion rot)
    {
        if (_skillData == null)
        {
            return;
        }

        // GameObject prefab = _skillData.EffectPrefab;

        //if (prefab == null)
        //{
        //    return;
        //}

        //GameObject fx;

        ////캐스터에 붙는 이펙트
        //if (_skillData.AttachType == EffectAttachType.Caster)
        //{
        //    fx = Instantiate(prefab, transform);
        //    fx.transform.localPosition = Vector3.zero;
        //    fx.transform.localRotation = Quaternion.identity;
        //}
        //else//월드 좌표 기준 이펙트
        //{
        //    fx = Instantiate(prefab, pos, rot);
        //}

        //fx.transform.localScale = Vector3.one * _skillData.EffectScale;

        //Destroy(fx, _skillData.EffectLifeTime);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHealEffect(NetworkId targetId)
    {
        if (_skillData == null)
        {
            return;
        }

        Debug.Log("RPC_PlayHealEffect 실행됨");
        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        //GameObject prefab = _skillData.EffectPrefab;

        //if (prefab == null)
        //{
        //    return;
        //}

        //Transform parent = null;

        //if (_skillData.AttachType == EffectAttachType.Target)
        //{
        //    parent = targetObj.transform;
        //}

        //GameObject effects = Instantiate(prefab, targetObj.transform.position, Quaternion.identity, parent);

        //effects.transform.localScale = Vector3.zero;
        //effects.transform.DOScale(_skillData.EffectScale, 0.5f).SetEase(Ease.OutBack);

        //Destroy(effects, _skillData.EffectLifeTime);
    }
}
