using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class HeroController : MobilityUnit, IBasicAttack
{

    [SerializeField] private string _heroId;
    [SerializeField] private NormalAttackDataSO _normalAttack;
    [SerializeField] private float _attackRange;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private UnitStat _unitStat;
    [SerializeField] private SkillDataSO _skillData;
    [SerializeField] private SkillDataSO _normalSkill;
    [SerializeField] private MonoBehaviour _skillComponent;
    [SerializeField] private HeroEffectRPC _rpc;

    [Header("타워 레퍼런스")]
    [SerializeField] private UnitBase _enemyTowerA;
    [SerializeField] private UnitBase _enemyTowerB;
    [SerializeField] private UnitBase _enemyBridge;

    private float _respawnTime;
    private AttackType _attackType;
    private GameObject _projectile;

    private UnitBase _currentTarget;

    private TickTimer _searchTimer;
    private TickTimer _attackTimer;
    private TickTimer _skillTimer;
    private UnitFSM _fsm;

    //배치
    private bool _isDeploying;
    private Vector3 _deployTarget;
    private TickTimer _deployFailSafeTimer;
    private IHeroSkill _currentSkill;

    public float AttackRange => _attackRange;
    public UnitBase CurrentTarget => _currentTarget;
    public float RespawnTime => _respawnTime;
    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float HealPower => _unitStat.HealPower.Value;
    public UnitStat UnitStat => _unitStat;
    public NavMeshAgent Agent => agent;
    public SkillDataSO SkillData => _skillData;
    public GameObject Projectile => _projectile;
    public Transform FirePoint => _firePoint;
    public HeroEffectRPC EffectRPC => _rpc;
    public LayerMask AllyLayer
    {
        get
        {
            return team == Team.Blue ? LayerMask.GetMask("BlueTeam") : LayerMask.GetMask("RedTeam");
        }
    }

    public void Setup(Team myTeam)
    {
        team = myTeam;
        base.Setup();

        UnitBase[] targetStructure = team == Team.Blue ?
            ObjectContainer.Instance.redSideStructure :
            ObjectContainer.Instance.blueSideStructure;

        _enemyTowerA = targetStructure[0];
        _enemyTowerB = targetStructure[1];
        _enemyBridge = targetStructure[2];
    }

    //스포너에서 전달받은 위치와 지연 시간을 기준으로 배치 시작
    //배치완료전 까지는 전투,FSM로직x
    public void BeginDeploy(Vector3 targetPos, float failSafeTime)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }
        _deployTarget = targetPos;
        _deployFailSafeTimer = TickTimer.CreateFromSeconds(Runner, failSafeTime);
        _isDeploying = true;
        MoveTo(_deployTarget);
    }

    private void FinishDeploy()//배치완료
    {
        _isDeploying = false;     
        agent.ResetPath();//배치 직후 바로 타겟 탐색 가능하도록 초기화
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _fsm?.ForceDetect();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (_rpc == null) { _rpc = GetComponent<HeroEffectRPC>(); }

        if (Object.HasStateAuthority)
        {
            networkedTeam = team;
        }

        _currentSkill = _skillComponent as IHeroSkill;

        unitType = UnitType.Hero;

        _attackType = _normalAttack.AttackType;
        _projectile = _normalAttack.EffectPrefab;

        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (_unitStat == null)
        {
            _unitStat = GetComponent<UnitStat>();
        }

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

        _fsm = new UnitFSM();

        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);

        CurrentState = UnitState.Idle;
    }

    public void EquipSkill(SkillDataSO newSkillData)
    {
        if (newSkillData == null)
        {
            return;
        }

        _skillData = newSkillData;

        if (_skillComponent == null)
        {
            return;
        }

        _currentSkill = _skillComponent as IHeroSkill;

        if (_currentSkill == null)
        {
            return;
        }

        _currentSkill.ChangeSkillData(_skillData);

        if (_skillTimer.IsRunning)
        {
            return;
        }

        _skillTimer = TickTimer.CreateFromSeconds(Runner, _skillData.InitCooldown);
    }


    public override void FixedUpdateNetwork()
    {
        // StateAuthority가 없는 클라이언트는 서버 상태를 그대로 반영만 함
        if (!Object.HasStateAuthority) return;

        // 사망 상태면 행동 중단
        if (CurrentState == UnitState.Dead) return;

        if (_fsm == null) { return; }

        if (_isDeploying)
        {
            //네비 도착 or 실제 거리 도착 둘중 하나면 탈출(고장 방지)
            float dist = Vector3.Distance(transform.position, _deployTarget);
            //콜라이더 크기에 따라 수치 수정해야 될수 있음
            if ((!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) || dist < 0.5f)
            {
                FinishDeploy();
                return;
            }
            //충돌로 배치가 너무 오래 걸리는 경우 배치 강제 종료
            if (_deployFailSafeTimer.Expired(Runner))
            {
                FinishDeploy();
                return;
            }

            return;
        }
        _currentSkill?.TickSkill(Runner);
        _fsm.TickSkill(Runner);

        //타겟이 죽었는데 아직 참조 남아있는 경우 즉시 재탐색
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            _currentTarget = null;
            _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        }

        //탐지 상태일 때만 타겟 재탐색
        if (_fsm.State == UnitAIState.Detect && _searchTimer.ExpiredOrNotRunning(Runner))
        {
            RefreshTarget();
            _searchTimer = TickTimer.CreateFromSeconds(Runner, SearchInterval);
        }

        bool hasTarget = _currentTarget != null && !_currentTarget.IsDead;
        bool inRange = false;
        if (hasTarget)
        {
            float combatDistance = GetAttackDistanceTo(_currentTarget);
            inRange = combatDistance <= _attackRange;
        }

        bool isDead = CurrentState == UnitState.Dead;//FSM에도 사망 여부를 전달(의도치 않은 상태 전이 방지)

        //FSM에 상태 전이 판단 위임
        _fsm.DecideState(isDead, hasTarget, inRange);

        //FSM 결과에 따라 행동 처리
        ApplyState(_fsm.State);
    }

    //FSM 결과에 따라 실제 유닛 행동을 적용 (AIState : 판단, UnitState : 애니메이션 등 표현)
    private void ApplyState(UnitAIState state)
    {
        switch (state)
        {
            case UnitAIState.Detect:
                HandleDetect();
                break;

            case UnitAIState.Attack:
                HandleAttack();
                break;

            case UnitAIState.Skill:
                HandleSkill();
                break;

            case UnitAIState.Dead:
                break;
        }
    }

    private void HandleDetect()
    {
        // 전투 타겟이 없는 경우
        if (_currentTarget == null)
        {
            //함교가 존재하면 계속 전진
            if (_enemyBridge != null)
            {
                MoveTo(_enemyBridge.transform.position);
                CurrentState = UnitState.Move;
            }
            else
            {
                StopMove();
                CurrentState = UnitState.Idle;
            }

            return;
        }

        //타겟이 있는 경우 해당 타겟을 향해 이동
        MoveTo(_currentTarget.transform.position);
        CurrentState = UnitState.Move;
    }

    private void HandleAttack()
    {
        StopMove();
        RotateToTarget();
        CurrentState = UnitState.Attack;
        HandleCombat();
    }

    private void HandleSkill()
    {
        StopMove();
        CurrentState = UnitState.Skill;
    }

    private void HandleCombat()
    {
        if (TryUseSkill()) { return; }

        //스킬 사용 불가시에 기본 공격
        TryAttack();
    }

    private void RotateToTarget()
    {
        if (_currentTarget == null)
        {
            return;
        }

        Vector3 dir = _currentTarget.transform.position - transform.position;
        dir.y = 0f; //수평 회전

        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        transform.rotation = targetRotation;
    }

    private bool TryUseSkill()//스킬 쿨타임/조건 체크 후 스킬 실행 시도
    {
        if (_currentSkill == null)
        {
            return false;
        }

        if (!_skillTimer.ExpiredOrNotRunning(Runner))
        {

            return false;
        }

        SkillRuntimeData runtime = _currentSkill.Data.CreateRuntimeData();

        if (!_currentSkill.CanUse(this, runtime))
        {
            return false;
        }

        bool success = _currentSkill.Execute(this, runtime);
        if (!success)
        {
            return false;
        }
        _fsm.EnterSkill(Runner, 0.15f);

        float cooldownReduction = Mathf.Clamp(_unitStat?.CooldownReduction?.Value ?? 0f, 0f, 0.9f);
        float finalCooldown = Mathf.Max(0.1f, runtime.Cooldown * (1f - cooldownReduction));

        _skillTimer = TickTimer.CreateFromSeconds(Runner, finalCooldown);

        return success;
    }

    private void TryAttack()
    {
        if (!_attackTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        if (_currentTarget == null)
        {
            return;
        }

        // IBasicAttack 인터페이스 기본 구현 호출 (target.TakeDamage(AttackPower))
        if (_attackType == AttackType.Melee)
        {
            ((IBasicAttack)this).BaseAttack(_currentTarget);
        }
        else
        {
            AttackRanged(_currentTarget.transform.position);
        }


        float attackCooldown = AttackSpeed > 0f ? 1f / AttackSpeed : 1f;
        _attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
    }

    private void RefreshTarget()
    {
        UnitBase enemy = FindTarget(); // MobilityUnit의 ITargetFinder 구현

        if (enemy != null)
        {
            SetTarget(enemy);
            return;
        }

        // 적 없으면 가까운 타워 선택
        UnitBase closestTower = GetClosestTower();
        if (closestTower != null)
        {
            SetTarget(closestTower);
        }
    }

    private void SetTarget(UnitBase target)
    {
        if (_currentTarget == target)
        {
            return;
        }

        // 새 목표로 교체할 때 기존 사망 이벤트 구독 해제
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= OnTargetDied;
        }

        _currentTarget = target;

        if (_currentTarget != null)
        {
            _currentTarget.OnDeath += OnTargetDied;
        }
    }

    private UnitBase GetClosestTower()
    {
        //두 타워 다 없으면 null
        if (_enemyTowerA == null && _enemyTowerB == null)
        {
            return null;
        }

        //둘 중 하나만 남았는데
        //내가 공격하던 타워가 아니라면
        //함교로 이동
        if (_enemyTowerA != null && _enemyTowerB == null)
        {
            if (_currentTarget == _enemyTowerA)
            {
                return _enemyTowerA;
            }

            return null;
        }

        if (_enemyTowerA == null && _enemyTowerB != null)
        {
            if (_currentTarget == _enemyTowerB)
            {
                return _enemyTowerB;
            }

            return null;
        }

        //둘 다 살아있으면 처음 선택한 타워 유지
        if (_currentTarget == _enemyTowerA)
        {
            return _enemyTowerA;
        }

        if (_currentTarget == _enemyTowerB)
        {
            return _enemyTowerB;
        }

        float distA = (transform.position - _enemyTowerA.transform.position).sqrMagnitude;
        float distB = (transform.position - _enemyTowerB.transform.position).sqrMagnitude;

        return distA <= distB ? _enemyTowerA : _enemyTowerB;
    }

    //Projectile 연출 및 기본 공격 데미지 적용
    private void AttackRanged(Vector3 targetPos)
    {
        if (_projectile == null || _firePoint == null)
        {
            return;
        }

        if (_currentTarget == null)
        {
            return;
        }
        Vector3 start = _firePoint.position;
        Vector3 end = _currentTarget.transform.position;

        EffectRPC.RPC_FireProjectile(start, end, ProjectileType.Normal, team);

        ApplyBasicAttackDamage(_currentTarget);
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

        target.TakeDamage(AttackPower);
    }

    public override void TakeDamage(float amount)
    {
        float reduction = _unitStat.DamageReduction.Value;

        if (reduction > 0f)
        {
            amount *= (1f - reduction);
        }
        base.TakeDamage(amount);
        Debug.Log($"[영웅체력] {name} : {CurrentHealth} / {MaxHealth}");
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

    

    //스킬증강에 사용될 메서드
    private void ApplySkillAugments()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        StageManager stageManager = FindFirstObjectByType<StageManager>();

        if (stageManager == null)
        {
            return;
        }

        if (!stageManager.PlayerDataMap.TryGet(Runner.LocalPlayer, out PlayerNetworkData data))
        {
            return;
        }

        //3.3 여현구
        //배열에서 구조체로 바뀌어서 여기 수정했습니다.
        for (int i = 0; i < SlotData_5.Length; i++)
        {
            string augmentId = data.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();

            if (string.IsNullOrEmpty(augmentId))
            {
                continue;
            }

            SkillAugmentSO so = AugmentController.Instance.GetSkillAugmentById(augmentId);
            if (so == null)
            {
                continue;
            }

            if (so.TargetHeroID != _heroId)
            {
                continue;
            }

            int tierIndex = data.TotalAugmentPicks >= 6 ? 1 : 0;

            if (tierIndex >= so.Tiers.Length)
            {
                continue;
            }

            SkillDataSO newSkill = so.Tiers[tierIndex].CombatSkillData;

            if (newSkill == null)
            {
                continue;
            }

            EquipSkill(newSkill);
        }
    }

    public void ForceStopMoveForSkill()//외부에서 StopMove를 사용가능하도록
    {
        StopMove();
    }

    private void OnTargetDied(UnitBase deadUnit)
    {
        deadUnit.OnDeath -= OnTargetDied;
        _currentTarget = null;

        // 타이머를 즉시 만료시켜 다음 틱에 바로 재탐색
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _fsm?.ForceDetect();
    }

    public override void Die()
    {
        _fsm?.ForceDead();
        StopMove();

        // 목표 이벤트 구독 해제 후 부모 Die 호출 (Despawn)
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= OnTargetDied;
            _currentTarget = null;
        }

        base.Die();
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // 탐지 범위 (노란 원)

        // 공격 범위 (빨간 원)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        // 현재 목표 → 미니언 연결선
        if (_currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }

    }
#endif
}
