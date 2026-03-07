using DG.Tweening;
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
    [SerializeField] protected MoveType moveType;

    [Header("전투 관련")]
    public float attackPower;
    public float attackRange;
    public float attackSpeed;
    public UnitBase currentTarget; // 현재 타겟
    public AttackType attackType;

    [Header("스탯 관련")]
    [SerializeField] private string _heroId;
    [SerializeField] private NormalAttackDataSO _normalAttack;
    [SerializeField] private float _attackRange;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private UnitStat _unitStat;
    [SerializeField] private SkillDataSO _skillData;
    [SerializeField] private SkillDataSO _normalSkill;
    [SerializeField] private MonoBehaviour _skillComponent;
    private float _respawnTime;

    [Header("타워 레퍼런스")]
    private UnitBase _enemyTowerA;
    private UnitBase _enemyTowerB;
    private UnitBase _enemyBridge;

    private GameObject _projectile;
    private IHeroSkill _currentSkill;
    private AttackType _attackType;

    // 상태 기계 및 상태 인스턴스들
    public StateMachine StateMachine { get; private set; }
    public DeployState DeployState { get; private set; }
    public DetectState DetectState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }
    public DieState DieState { get; private set; }

    public float AttackRange => _attackRange;
    public UnitBase CurrentTarget => currentTarget;
    public float RespawnTime => _respawnTime;
    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float HealPower => _unitStat.HealPower.Value;
    public UnitStat UnitStat => _unitStat;
    public NavMeshAgent Agent => agent;
    public SkillDataSO SkillData => _skillData;
    public GameObject Projectile => _projectile;
    public Transform FirePoint => _firePoint;
    public LayerMask TargetLayer => targetLayer;

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

        _currentSkill = _skillComponent as IHeroSkill;
        _attackType = _normalAttack.AttackType;
        _projectile = _normalAttack.EffectPrefab;

        if (!Object.HasStateAuthority) return;
        
        // === 이 아래론 마스터 클라이언트가 아니면 실행되지 않음. ===
        // 상태 인스턴스 생성
        StateMachine = new StateMachine();
        DeployState = new DeployState(this);
        DetectState = new DetectState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
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

        //스킬
        EquipSkill(_normalSkill);
        ApplySkillAugments();

        if (agent != null)
        {
            agent.enabled = false;

            //// 스폰 위치가 네비 밖이거나 겹쳐있을 경우 보정
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }


            agent.enabled = true;
            agent.ResetPath();
        }

        StateMachine.ChangeState(DeployState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (IsDead) return; // 사망 시 중단 (혹은 DieState에서 처리)

        StateMachine.Update();
    }

    // --- 생성시 초기화 관련 메서드 ---

    public void Setup(Team myTeam)
    {
        team = myTeam;
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

    public void EquipSkill(SkillDataSO newSkillData)
    {
        if (newSkillData == null)
            return;

        _skillData = newSkillData;

        if (_skillComponent == null)
            return;

        _currentSkill = _skillComponent as IHeroSkill;

        if (_currentSkill == null)
            return;

        _currentSkill.ChangeSkillData(_skillData);
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

            EquipSkill(newSkill);
        }
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

    //Projectile 연출 및 기본 공격 데미지 적용
    public void AttackRanged(Vector3 targetPos)
    {
        if (_projectile == null || _firePoint == null) return;
        if (currentTarget == null) return;

        Vector3 start = _firePoint.position;
        Vector3 end = currentTarget.transform.position;

        RPC_FireProjectile(Object.Id, currentTarget.Object.Id, team, false);

        ApplyBasicAttackDamage(currentTarget);
    }

    //모든 기본 공격은 이 메서드를 통해 TakeDamage로 진입
    private void ApplyBasicAttackDamage(UnitBase target)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(attackPower);
    }

    public void HealUnit(UnitBase target, float healAmount)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (target == null || target.IsDead)
        {
            return;
        }

        target.CurrentHealth = Mathf.Min(
            target.CurrentHealth + healAmount,
            target.MaxHealth
        );
    }

    //포격형 전용 데미지 진입점
    public void ApplyBarrageSkillDamage(UnitBase target, float damageRatio)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (target == null || target.IsDead)
        {
            return;
        }

        float baseDamage = AttackPower;
        float finalDamage = baseDamage * damageRatio;

        target.TakeDamage(finalDamage);
    }

    public void ForceStopMoveForSkill()//외부에서 StopMove를 사용가능하도록
    {
        StopMove();
    }

    //기본 공격
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FireProjectile(NetworkId casterId, NetworkId targetId, Team team, bool isSkill)
    {
        if (!Runner.TryFindObject(casterId, out NetworkObject casterObj))
        {
            return;
        }

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        NewHeroController hero = casterObj.GetComponent<NewHeroController>();
        if (hero == null)
        {
            return;
        }

        GameObject projectilePrefab = null;

        if (isSkill)
        {
            if (hero._skillData == null)
            {
                return;
            }

            projectilePrefab = hero._skillData.EffectPrefab;
        }
        else
        {
            projectilePrefab = hero._projectile;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        if (hero._firePoint == null)
        {
            return;
        }

        Vector3 start = hero._firePoint.position;
        Vector3 end = targetObj.transform.position;

        GameObject projectileObj = Instantiate(projectilePrefab, start, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Fire(end, team);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySkillEffect(Vector3 pos, Quaternion rot)
    {
        if (_skillData == null)
        {
            return;
        }

        GameObject prefab = _skillData.EffectPrefab;

        if (prefab == null)
        {
            return;
        }

        GameObject fx;

        //캐스터에 붙는 이펙트
        if (_skillData.AttachType == EffectAttachType.Caster)
        {
            fx = Instantiate(prefab, transform);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
        }
        else//월드 좌표 기준 이펙트
        {
            fx = Instantiate(prefab, pos, rot);
        }

        fx.transform.localScale = Vector3.one * _skillData.EffectScale;

        Destroy(fx, _skillData.EffectLifeTime);
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

        GameObject prefab = _skillData.EffectPrefab;

        if (prefab == null)
        {
            return;
        }

        Transform parent = null;

        if (_skillData.AttachType == EffectAttachType.Target)
        {
            parent = targetObj.transform;
        }

        GameObject effects = Instantiate(prefab, targetObj.transform.position, Quaternion.identity, parent);

        effects.transform.localScale = Vector3.zero;
        effects.transform.DOScale(_skillData.EffectScale, 0.5f).SetEase(Ease.OutBack);

        Destroy(effects, _skillData.EffectLifeTime);
    }
}
