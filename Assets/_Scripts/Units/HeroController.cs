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
    [SerializeField] private float _skillRange = 4f;
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

    [Header("배치 설정")]
    [SerializeField] private float _minDeployTime = 0.25f;
    [SerializeField] private float _maxDeployTime = 2.5f;

    //스테이지 기준 거리(임시)
    [SerializeField] private float _minDeployDistance = 1f;
    [SerializeField] private float _maxDeployDistance = 15f;

    private UnitBase _currentTarget;

    private TickTimer _searchTimer;
    private TickTimer _attackTimer;
    private TickTimer _skillTimer;
    private UnitFSM _fsm;

    //배치
    private bool _isDeploying;
    private Vector3 _deployTarget;
    private TickTimer _deployDelayTimer;
    private Transform _deployOrigin;

    public float AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;

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

        UnitBase[] myStructures = team == Team.Blue ? 
            ObjectContainer.Instance.blueSideStructure : 
            ObjectContainer.Instance.redSideStructure;

        _deployOrigin = myStructures[2].transform;
    }

    public void StartDeploy(Vector3 position)//배치중
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        _deployTarget = position;

        //함교 기준 거리 계산
        float distance = Vector3.Distance(_deployOrigin.transform.position, _deployTarget);

        //배치 시간 변환
        float deployTime = GetDeployDelay(distance);

        _deployDelayTimer = TickTimer.CreateFromSeconds(Runner, deployTime);
        _isDeploying = true;
    }

    private float GetDeployDelay(float distance)
    {
        float time = Mathf.InverseLerp(_minDeployDistance, _maxDeployDistance, distance);

        return Mathf.Lerp(_minDeployTime, _maxDeployTime, time);
    }

    private void FinishDeploy()//배치완료
    {
        _isDeploying = false;
        _deployDelayTimer = default;
    }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority) return;

        _fsm = new UnitFSM();

        // 타이머를 즉시 만료 상태로 초기화 -> 첫 틱에 곧바로 탐색 실행
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _skillTimer = TickTimer.CreateFromSeconds(Runner, _skillCooldown);

        if (_isDeploying)
        {
            CurrentState = UnitState.Move;
        }
        else
        {
            CurrentState = UnitState.Idle;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // StateAuthority가 없는 클라이언트는 서버 상태를 그대로 반영만 함
        if (!Object.HasStateAuthority) return;

        // 사망 상태면 행동 중단
        if (CurrentState == UnitState.Dead) return;

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

        //탐지 상태일 때만 타겟 재탐색 (공격 중에는 중단)
        if (_fsm.State == UnitAIState.Detect && _searchTimer.ExpiredOrNotRunning(Runner))
        {
            RefreshTarget();
            _searchTimer = TickTimer.CreateFromSeconds(Runner, SearchInterval);
        }

        bool hasTarget = _currentTarget != null;
        bool inRange = hasTarget && Vector3.Distance(transform.position, _currentTarget.transform.position) <= AttackRange;
        bool isDead = CurrentState == UnitState.Dead;//FSM에도 사망 여부를 전달(의도치 않은 상태 전이 방지)

        //FSM에 상태 전이 판단 위임
        _fsm.DecideState(isDead, hasTarget, inRange);

        //FSM 결과에 따라 행동 처리
        ApplyState(_fsm.State);
        //switch (_fsm.State)
        //{
        //    case UnitAIState.Detect:
        //        CurrentState = UnitState.Move;
        //        if (_currentTarget != null)
        //        {
        //            MoveTo(_currentTarget.transform.position);
        //        }
        //        break;

        //    case UnitAIState.Attack:
        //        CurrentState = UnitState.Attack;
        //        StopMove();
        //        HandleCombat();
        //        break;

        //    case UnitAIState.Dead:
        //        CurrentState = UnitState.Dead;
        //        StopMove();
        //        break;
        //}
    }

    //FSM 결과에 따라 실제 유닛 행동을 적용 (AIState : 판단, UnitState : 애니메이션등 표현)
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

            case UnitAIState.Dead:
                HandleDead();
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

        //타겟이 있는 경우 → 해당 타겟을 향해 이동
        CurrentState = UnitState.Move;
        MoveTo(_currentTarget.transform.position);
    }

    private void HandleAttack()
    {
        CurrentState = UnitState.Attack;
        StopMove();
        HandleCombat();
    }

    private void HandleDead()
    {
        CurrentState = UnitState.Dead;
        StopMove();
    }

    private void HandleCombat()
    {
        if (_currentTarget == null)
        {
            return;
        }

        //스킬 사용 가능하면 우선 시도
        if (CanUseSkill())
        {
            if (IsSkillTargetInRange())
            {
                UseSkill();
                return;
            }
            else
            {
                //스킬 조건 충족을 위해 이동
                MoveTo(_currentTarget.transform.position);
                return;
            }
        }

        //스킬 사용 불가 → 기본 공격
        TryAttack();
    }

    private bool CanUseSkill()
    {
        return _skillTimer.ExpiredOrNotRunning(Runner);
    }

    private bool IsSkillTargetInRange()
    {
        if (_currentTarget == null)
        {
            return false;
        }

        float dist = Vector3.Distance(
            transform.position,
            _currentTarget.transform.position
        );

        return dist <= _skillRange;
    }

    private void UseSkill()
    {
        if (_currentTarget == null)
        {
            return;
        }

        CurrentState = UnitState.Skill;//애니메이션 연출등등

        //임시 구현 (지금은 데미지 2배 스킬이라고 가정)
        _currentTarget.TakeDamage(_attackPower * 2f);

        _skillTimer = TickTimer.CreateFromSeconds(Runner, _skillCooldown);
    }

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

    //private void HandleBehaviour()
    //{
    //    // 유효한 목표가 없으면 대기 (함교까지 다 부쉈을 때)
    //    if (_currentTarget == null)
    //    {
    //        CurrentState = UnitState.Idle;
    //        StopMove();
    //        return;
    //    }

    //    float distToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

    //    if (distToTarget > AttackRange)
    //    {
    //        // ── 이동 ──
    //        CurrentState = UnitState.Move;
    //        MoveTo(_currentTarget.transform.position);
    //    }
    //    else
    //    {
    //        // ── 공격 ──
    //        StopMove();
    //        CurrentState = UnitState.Attack;
    //        TryAttack();
    //    }
    //}
    

    //private void AttackMelee(Transform target)
    //{
    //    Tower tower = target.GetComponent<Tower>();
    //    if (tower != null)
    //    {
    //        tower.TakeDamage(AttackPower);
    //        return;
    //    }

    //    UnitBase unit = target.GetComponent<UnitBase>();
    //    if (unit != null)
    //    {
    //        unit.TakeDamage(AttackPower);
    //    }
    //}

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

        //배치 디버그
        if (_deployOrigin != null)
        {
            // 최소 배치 거리 (빠른 배치)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_deployOrigin.transform.position, _minDeployDistance);

            // 최대 배치 거리 (느린 배치)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_deployOrigin.transform.position, _maxDeployDistance);

            // 현재 배치 타겟 표시 (배치 중일 때)
            if (_isDeploying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_deployOrigin.transform.position, _deployTarget);
                Gizmos.DrawSphere(_deployTarget, 0.2f);
            }
        }
    }
#endif
}
