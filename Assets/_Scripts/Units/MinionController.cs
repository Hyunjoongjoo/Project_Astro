using Fusion;
using UnityEngine;

public enum AttackType
{
    Melee, Range
}
public class MinionController : MobilityUnit, IBasicAttack
{
    [Header("공격 스테이터스")]
    [SerializeField] private float _attackPower = 10f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 2f;

    [Header("공격 타입")]
    [SerializeField] private AttackType _attackType;

    [Header("원거리")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("타워 레퍼런스")]
    [SerializeField] private UnitBase _towerA;
    [SerializeField] private UnitBase _towerB;
    [SerializeField] private UnitBase _bridge;

    private UnitBase _currentTarget;

    private TickTimer _searchTimer;
    private TickTimer _attackTimer;
    private UnitFSM _fsm;

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

        _towerA = targetStructure[0];
        _towerB = targetStructure[1];
        _bridge = targetStructure[2];
    }

    public void SetAttackType(AttackType attackType)
    {
        _attackType = attackType;
    }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority) return;

        _fsm = new UnitFSM();

        //타이머를 즉시 만료 상태로 초기화 -> 첫 틱에 곧바로 탐색 실행
        _searchTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _attackTimer = TickTimer.CreateFromSeconds(Runner, 0f);

        CurrentState = UnitState.Move;
    }

    public override void FixedUpdateNetwork()
    {
        // StateAuthority가 없는 클라이언트는 서버 상태를 그대로 반영만 함
        if (!Object.HasStateAuthority) return;

        // 사망 상태면 행동 중단
        if (CurrentState == UnitState.Dead) return;

        // 주기적 탐색
        if (_fsm.State == UnitAIState.Detect && _searchTimer.ExpiredOrNotRunning(Runner))
        {
            RefreshTarget();
            _searchTimer = TickTimer.CreateFromSeconds(Runner, SearchInterval);
        }

        bool hasTarget = _currentTarget != null;
        bool inRange = hasTarget && Vector3.Distance(transform.position, _currentTarget.transform.position) <= AttackRange;
        bool isDead = CurrentState == UnitState.Dead;//FSM에도 사망 여부를 전달(의도치 않은 상태 전이 방지)

        //FSM에 상태 전이 판단 위임
        _fsm.DecideState(isDead: isDead, hasTarget: hasTarget, inRange: inRange);

        //FSM 결과에 따라 행동 처리
        ApplyState(_fsm.State);
        //switch (_fsm.State)
        //{
        //    case UnitAIState.Detect:
        //        CurrentState = UnitState.Move;
        //        OnDetectUpdate();
        //        break;

        //    case UnitAIState.Attack:
        //        CurrentState = UnitState.Attack;
        //        OnAttackUpdate();
        //        break;

        //    case UnitAIState.Dead:
        //        CurrentState = UnitState.Dead;
        //        StopMove();
        //        break;
        //}
    }

    // FSM 판단 결과를 실제 유닛 행동으로 적용
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
                break;
        }
    }

    private void HandleDetect()
    {
        // 전투 타겟이 없는 경우
        if (_currentTarget == null)
        {
            //함교가 존재하면 계속 전진
            if (_bridge != null)
            {
                CurrentState = UnitState.Move;
                MoveTo(_bridge.transform.position);
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
        TryAttack();
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
        if (_bridge == null) return null;

        // 두 타워가 모두 없으면 함교
        if (_towerA == null && _towerB == null) return _bridge;

        // 타워가 하나만 남은 경우 타워나 함교 중 가까운 쪽
        if (_towerA == null)
        {
            float distTower = Vector3.Distance(transform.position, _towerB.transform.position);
            float distBridge = Vector3.Distance(transform.position, _bridge.transform.position);
            return distBridge < distTower ? _bridge : _towerB;
        }

        if (_towerB == null)
        {
            float distTower = Vector3.Distance(transform.position, _towerA.transform.position);
            float distBridge = Vector3.Distance(transform.position, _bridge.transform.position);
            return distBridge < distTower ? _bridge : _towerA;
        }

        // 두 타워가 모두 살아있다면 둘 중 가까운 넘
        float distA = Vector3.Distance(transform.position, _towerA.transform.position);
        float distB = Vector3.Distance(transform.position, _towerB.transform.position);
        return distA <= distB ? _towerA : _towerB;
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

    //private void OnDetectUpdate()
    //{
    //    if (_currentTarget == null)
    //    {
    //        StopMove();
    //        return;
    //    }

    //    MoveTo(_currentTarget.transform.position);
    //}

    //private void OnAttackUpdate()
    //{
    //    StopMove();
    //    TryAttack();
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

    //투사체(현재는 이펙트만)
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
    }
#endif
}
