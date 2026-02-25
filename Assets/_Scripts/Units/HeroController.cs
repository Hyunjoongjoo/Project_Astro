using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class HeroController : MobilityUnit, IBasicAttack
{
    [Header("공격 스테이터스")]
    [SerializeField] private float _attackPower = 10f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 2f;

    [Header("스킬 스테이터스")]
    //[SerializeField] private float _skillRange = 4f;
    [SerializeField] private float _skillCooldown = 6f;

    [Header("공격 타입")]
    [SerializeField] private AttackType _attackType;

    [Header("원거리")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("타워 레퍼런스")]
    [SerializeField] private UnitBase _enemyTowerA;
    [SerializeField] private UnitBase _enemyTowerB;
    [SerializeField] private UnitBase _enemyBridge;

    [SerializeField] private MonoBehaviour _skillComponent;


    private UnitBase _currentTarget;

    private TickTimer _searchTimer;
    private TickTimer _attackTimer;
    private TickTimer _skillTimer;
    private UnitFSM _fsm;

    //배치
    private bool _isDeploying;
    private Vector3 _deployTarget;
    private TickTimer _deployDelayTimer;

    private IHeroSkill _skill; //영웅별로 서로 다른 스킬을 처리하기 위한 스킬 인터페이스
    private UnitBase _skillTarget;

    public float AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;
    public UnitBase CurrentTarget => _currentTarget;
    public UnitBase SkillTarget => _skillTarget;

    private void Awake()
    {
        // 인스펙터에서 할당된 스킬 컴포넌트를 IHeroSkill 인터페이스로 캐스팅하여 사용
        _skill = _skillComponent as IHeroSkill;
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
    //배치완료전 까지는 전투,FSM로직 동작x
    public void BeginDeploy(Vector3 targetPos, float deployDelay)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        _deployTarget = targetPos;
        _deployDelayTimer = TickTimer.CreateFromSeconds(Runner, deployDelay);
        _isDeploying = true;
    }

    private void FinishDeploy()//배치완료
    {
        _isDeploying = false;
        _deployDelayTimer = default;
        //배치 직후 바로 타겟 탐색 가능하도록 초기화
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority) return;

        _fsm = new UnitFSM();

        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _skillTimer = TickTimer.CreateFromSeconds(Runner, _skillCooldown);

        CurrentState = UnitState.Idle;
    }

    public override void FixedUpdateNetwork()
    {
        // StateAuthority가 없는 클라이언트는 서버 상태를 그대로 반영만 함
        if (!Object.HasStateAuthority) return;

        // 사망 상태면 행동 중단
        if (CurrentState == UnitState.Dead) return;

        _fsm.TickSkill(Runner);

        if (_isDeploying)
        {
            //배치중 일때는
            if (!_deployDelayTimer.Expired(Runner))
            {
                return;
            }

            MoveTo(_deployTarget);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                FinishDeploy();
            }
            return;
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
            //이동 중인지 여부 (네비 기준)
            bool isApproaching = agent.hasPath && !agent.pathPending;

            //실제 전투 거리 판정은 콜라이더 기준
            float combatDistance = GetAttackDistanceTo(_currentTarget);
            bool isCombatInRange = combatDistance <= _attackRange;

            inRange = isApproaching && isCombatInRange;
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
                CurrentState = UnitState.Move;
                MoveTo(_enemyBridge.transform.position);
            }
            else
            {
                CurrentState = UnitState.Idle;
                StopMove();
            }

            return;
        }

        //타겟이 있는 경우 해당 타겟을 향해 이동
        CurrentState = UnitState.Move;
        MoveTo(_currentTarget.transform.position);
    }

    private void HandleAttack()
    {
        CurrentState = UnitState.Attack;
        StopMove();
        RotateToTarget();
        HandleCombat();
    }

    private void HandleSkill()
    {
        CurrentState = UnitState.Skill;
        StopMove();
    }

    private void HandleCombat()
    {
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            return;
        }

        //스킬 시작 조건 판단
        if (_fsm.State == UnitAIState.Attack && _skill != null && CanUseSkill() && _skill.CanUse(this))
        {
            StartSkill();//여기서 FSM 전환
            return;
        }

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

    private void StartSkill()
    {
        _skillTarget = _currentTarget;
        _fsm.EnterSkill(Runner, 0.15f);// FSM 상태 전환

        UseSkill();// 실제 효과
    }

    private void UseSkill()
    {
        if (_skillTarget == null || _skillTarget.IsDead)
        {
            _skillTarget = null;
            return;
        }

        CurrentState = UnitState.Skill;//애니메이션 연출등등
        StopMove();
        
        _skill.Execute(this);
        _skillTimer = TickTimer.CreateFromSeconds(Runner, _skillCooldown);

        _skillTarget = null;
    }


    private bool CanUseSkill()
    {
        return _skillTimer.ExpiredOrNotRunning(Runner);
    }

    //private bool IsSkillTargetInRange()
    //{
    //    if (_currentTarget == null)
    //    {
    //        return false;
    //    }

    //    float dist = Vector3.Distance(
    //        transform.position,
    //        _currentTarget.transform.position
    //    );

    //    return dist <= _skillRange;
    //}

    private void TryAttack()
    {
        if (!_attackTimer.ExpiredOrNotRunning(Runner)) return;
        if (_currentTarget == null) return;

        // IBasicAttack 인터페이스 기본 구현 호출 (target.TakeDamage(AttackPower))
        if (_attackType == AttackType.Melee)
        {
            ((IBasicAttack)this).BaseAttack(_currentTarget);
        }
        else
        {
            AttackRanged(_currentTarget.transform.position);
        }

        // 다음 공격 가능 시간 설정 (AttackSpeed = 초당 공격 횟수)
        float cooldown = AttackSpeed > 0f ? 1f / AttackSpeed : 1f;
        _attackTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
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
        // 함교도 없으면 타겟 없음 (사실상 게임 종료)
        if (_enemyBridge == null) return null;

        // 두 타워가 모두 없으면 함교
        if (_enemyTowerA == null && _enemyTowerB == null) return _enemyBridge;

        // 타워가 하나만 남은 경우 타워나 함교 중 가까운 쪽
        if (_enemyTowerA == null)
        {
            float distTower = Vector3.Distance(transform.position, _enemyTowerB.transform.position);
            float distBridge = Vector3.Distance(transform.position, _enemyBridge.transform.position);
            return distBridge < distTower ? _enemyBridge : _enemyTowerB;
        }

        if (_enemyTowerB == null)
        {
            float distTower = Vector3.Distance(transform.position, _enemyTowerA.transform.position);
            float distBridge = Vector3.Distance(transform.position, _enemyBridge.transform.position);
            return distBridge < distTower ? _enemyBridge : _enemyTowerA;
        }

        // 두 타워가 모두 살아있다면 둘 중 가까운 넘
        float distA = Vector3.Distance(transform.position, _enemyTowerA.transform.position);
        float distB = Vector3.Distance(transform.position, _enemyTowerB.transform.position);
        return distA <= distB ? _enemyTowerA : _enemyTowerB;
    }

 
    private void AttackRanged(Vector3 targetPos)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        if (_currentTarget == null)
        {
            return;
        }

        _currentTarget.TakeDamage(AttackPower);

        RPC_FireProjectile(targetPos);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FireProjectile(Vector3 targetPos)
    {
        if (_projectilePrefab == null || _firePoint == null)
        {
            return;
        }

        GameObject projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);

        projectile.GetComponent<Projectile>()?.Fire(targetPos);
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    //public void RPC_PlaySkillEffect(Vector3 position, float radius)
    //{
    //    if (_skillEffectPrefab == null)
    //    {
    //        return;
    //    }

    //    GameObject fx = Instantiate(_skillEffectPrefab, position, Quaternion.identity);
    //    fx.GetComponent<AssaultSkill>()?.Play(radius);
    //}

    public void ForceStopMoveForSkill()//외부에서 스탑무브를 사용가능하도록
    {
        StopMove();
    }

    private void OnTargetDied(UnitBase deadUnit)
    {
        deadUnit.OnDeath -= OnTargetDied;
        _currentTarget = null;

        // 타이머를 즉시 만료시켜 다음 틱에 바로 재탐색
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
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
