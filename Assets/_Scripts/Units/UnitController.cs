using Fusion;
using System;
using UnityEngine;
using UnityEngine.AI;

public class UnitController : UnitBase
{
    [Header("내비게이션 및 탐지")]
    public NavMeshAgent agent;
    public LayerMask targetLayer;
    public float searchInterval = 0.3f;
    [SerializeField] protected MoveType moveType;

    [Header("스탯 관련")]
    [HideInInspector] public float attackRange;
    [SerializeField] protected string unitId;
    public Transform firePoint;

    [HideInInspector] public UnitBase currentTarget;

    protected UnitBase _towerA;
    protected UnitBase _towerB;
    protected UnitBase _bridge;

    public ISkill normalAttack;

    [Networked] public bool networkedOnHit { get; set; }

    [Header("스킬 데이터")]
    [Header("평타 공격")]
    [SerializeField] protected BaseSkillSO _normalAttackData;
    
    // 상태 머신과 상태 인스턴스들
    public StateMachine StateMachine { get; protected set; }
    public DetectState DetectState { get; protected set; }
    public ChaseState ChaseState { get; protected set; }
    public AttackState AttackState { get; protected set; }
    public DieState DieState { get; protected set; }

    public Animator HeroAnimator { get; protected set; }
    public Animator BoosterAnimator { get; protected set; }
    [Networked] public bool BoosterRender { get; set; }

    //stat 실시간 참조
    public float AttackPower => _unitStat.Attack.Value;
    public float AttackSpeed => _unitStat.AttackSpeed.Value;
    public float HealPower => _unitStat.HealPower.Value;//나중에 힐스킬에 연결
    public float MoveSpeed => _unitStat.MoveSpeed.Value;
    public float DetectRange => _unitStat.DetectRange.Value;
    
    public UnitBase CurrentTarget => currentTarget;
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

    protected readonly float MIN_ATTACK_RANGE = 2f;

    public override void Spawned()
    {
        base.Spawned();

        unitType = UnitType.Minion;

        normalAttack = _normalAttackData.CreateInstance(this);

        InitAttackRange();

        if (!Object.HasStateAuthority) return;

        // 상태 인스턴스 생성
        StateMachine = new StateMachine();
        DetectState = new DetectState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        DieState = new DieState(this);

        _unitStat = GetComponent<UnitStat>();

        UnitData data = TableManager.Instance.UnitTable.Get(unitId);
        moveType = data.moveType;
        //UnitStat 초기화
        _unitStat.Init(data);

        //Stat 기반 값 적용
        MaxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = MaxHealth;
        //moveSpeed = _unitStat.MoveSpeed.Value;
        //searchRange = _unitStat.DetectRange.Value;
        agent.speed = MoveSpeed;
        ConfigureAreaMask();
        StateMachine.ChangeState(DetectState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        StateMachine.Update();

        if (normalAttack.IsCasting)
            normalAttack.Tick();
    }

    // --- 생성시 초기화 관련 메서드 ---

    // 스폰 전에 실행되는 메서드

    public void Setup(Team myTeam)
    {
        team = myTeam;
        agent = GetComponent<NavMeshAgent>();

        //ConfigureAreaMask();

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

    //공격 사거리 초기화 메서드
    protected void InitAttackRange()
    {
        if (_normalAttackData is ProjectileSkillSO projectileSO)
        {
            attackRange = Mathf.Max(projectileSO.range, MIN_ATTACK_RANGE);
        }
        else if(_normalAttackData is HitscanSkillSO hitscanSO)
        {

            attackRange = Mathf.Max(hitscanSO.range, MIN_ATTACK_RANGE);
        }
    }

    // --- 유틸리티 메서드 (상태 클래스들에서 호출해서 사용) ---

    public void RotateToTarget(Vector3 direction)
    {
        direction.y = 0f; // 수평 회전만 적용
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
        if (UnitType == UnitType.Hero)
        {
            var hero = this as HeroController;
            if (hero != null && hero.StateMachine != null)
            {
                if (hero.StateMachine.CurrentState != hero.DeployState)
                {
                    if (Object != null && Object.IsValid && Object.HasStateAuthority)
                    {
                        BoosterRender = true;
                    }
                }
            }
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
        if (UnitType == UnitType.Hero)
        {
            if (Object != null && Object.IsValid && Object.HasStateAuthority)
            {
                BoosterRender = false;
            }
        }
    }

    public UnitBase FindTarget()
    {
        // 기존 MobilityUnit의 OverlapSphere 탐색 로직
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectRange, targetLayer);
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

    // 주변에서 가장 가까운 혹은 먼 적 영웅 탐색
    public UnitBase FindOnlyHeroTarget(Collider[] overlapResults, float range, bool isFar = false) 
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            range,
            overlapResults,
            TargetLayer
        );

        UnitBase targetHero = null;

        // isFar가 true면 가장 멀리 있는 적. false면 가까이 있는 적.
        float bestSqrDist = isFar ? float.MinValue : float.MaxValue;

        // isFar 여부에 따라 값 비교를 델리게이트로 정의
        Func<float, float, bool> isBetter = isFar
            ? (cur, best) => cur > best
            : (cur, best) => cur < best;

        for (int i = 0; i < hitCount; i++)
        {
            Collider overlapCollider = overlapResults[i];
            if (overlapCollider == null) continue;
            if (!overlapCollider.TryGetComponent(out UnitBase targetUnit)) continue;
            if (targetUnit == this) continue;
            if (targetUnit.IsDead) continue;
            if (targetUnit.Object == null) continue;
            if (targetUnit.team == team) continue;
            if (targetUnit.UnitType != UnitType.Hero) continue;

            float curSqrDist = (transform.position - targetUnit.transform.position).sqrMagnitude;

            if (isBetter(curSqrDist, bestSqrDist))
            {
                bestSqrDist = curSqrDist;
                targetHero = targetUnit;
            }
        }

        return targetHero;
    }

    public UnitBase GetClosestTower()
    {
        // 함교가 없다면 게임이 끝난 상태니 행동 중지
        if (_bridge == null)
            return null;

        //두 타워 다 없으면 함교
        if (_towerA == null && _towerB == null)
            return _bridge;

        // 살아있는 건물들 중 가장 가까운 것을 반환
        UnitBase closest = _bridge;
        float closestDist = (transform.position - _bridge.transform.position).sqrMagnitude;

        if (_towerA != null)
        {
            float distA = (transform.position - _towerA.transform.position).sqrMagnitude;
            if (distA < closestDist)
            {
                closest = _towerA;
                closestDist = distA;
            }
        }

        if (_towerB != null)
        {
            float distB = (transform.position - _towerB.transform.position).sqrMagnitude;
            if (distB < closestDist)
            {
                closest = _towerB;
                closestDist = distB;
            }
        }

        return closest;
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
        AudioClip clip = null;

        // 평타 공격인가 스킬인가
        if (type == SkillType.NormalAttack)
        {
            prefab = unit._normalAttackData.skillVFX;
            clip = unit._normalAttackData.skillSFX;
        }
        else if (hero != null)
        {
            prefab = hero.GetSkillVFX(type);
            clip = hero.curUniqueSkill.Data.skillSFX;
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
        AudioManager.Instance.PlaySfx(clip);

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
            if (type == SkillType.NormalAttack)
            {
                projectileSO = unit.normalAttack.Data as ProjectileSkillSO;
                if (networkedOnHit == true)
                    prefab = (unit as HeroController).CurUniqueSkill.Data.skillVFX;
                else
                    prefab = projectileSO.skillVFX;
            }

            else
            {
                if (unit is HeroController)
                {
                    projectileSO = ((unit as HeroController).CurUniqueSkill.Data) as ProjectileSkillSO;
                    prefab = projectileSO.skillVFX;
                }
            }

            if (projectileSO == null)
                return;

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
                AudioManager.Instance.PlaySfx(projectileSO.skillSFX);
                projectile.Fire(targetPos);
            }
        }
    }

    //-------테슬라 평타, 스킬 관련 RPC----------//

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHitscanEffect(NetworkId fromId, NetworkId toId)
    {
        if (!Runner.TryFindObject(fromId, out NetworkObject fromObj)) return;
        if (!Runner.TryFindObject(toId, out NetworkObject toObj)) return;

        UnitController unit = fromObj.GetComponent<UnitController>();

        // 시작 위치
        Vector3 start = unit.firePoint != null ? unit.firePoint.position : fromObj.transform.position;
        Vector3 end = toObj.transform.position;// 시작 위치

        HitscanSkillSO hitscanSO = unit.normalAttack.Data as HitscanSkillSO;
        if (hitscanSO == null) return;

        GameObject prefab = hitscanSO.skillVFX;
        AudioClip clip = hitscanSO.skillSFX;
        if (prefab == null) return;

        // 타겟 위치
        Vector3 dir = end - start;
        float distance = dir.magnitude;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = fromObj.transform.forward;
        }
        else
        {
            dir = dir.normalized;
        }

        // 최소 길이 보정 (근거리 가시성 확보)
        float minLength = 1.5f;

        if (distance < minLength)
        {
            Vector3 center = (start + end) * 0.5f;

            start = center - dir * (minLength * 0.5f);
            end = center + dir * (minLength * 0.5f);

            distance = minLength;
        }

        // 이펙트 생성
        AudioManager.Instance.PlaySfx(clip);
        GameObject fx = Instantiate(prefab);
        LineRenderer lr = fx.GetComponent<LineRenderer>();

        if (lr != null)
        {
            lr.useWorldSpace = true;

            int count = 4; // 포인트 개수
            lr.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);

                // 중간 포인트만 흔들림 (가시성 보정)
                if (i != 0 && i != count - 1)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.2f;
                    pos += new Vector3(offset.x, 0f, offset.y);
                }

                lr.SetPosition(i, pos);
            }
        }

        Destroy(fx, 0.3f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayChainEffect(NetworkId fromId, NetworkId toId)
    {
        if (!Runner.TryFindObject(fromId, out NetworkObject fromObj)) return;
        if (!Runner.TryFindObject(toId, out NetworkObject toObj)) return;

        UnitController unit = fromObj.GetComponent<UnitController>();

        HeroController hero = unit as HeroController;
        if (hero == null || hero.CurUniqueSkill == null) return;

        ChainSkillSO chainSkillSO = hero.CurUniqueSkill.Data as ChainSkillSO;
        if (chainSkillSO == null) return;

        GameObject prefab = chainSkillSO.skillVFX;
        AudioClip clip = chainSkillSO.skillSFX;
        if (prefab == null) return;

        // 시작 위치
        Vector3 start = unit.firePoint != null ? unit.firePoint.position : fromObj.transform.position;

        Vector3 end = toObj.transform.position;// 타겟 위치
        Collider targetCollider = toObj.GetComponent<Collider>(); // 타겟 collider 기준 위치 보정
        if (targetCollider != null)
        {
            end = targetCollider.ClosestPoint(start);
        }

        // 방향 / 거리 계산
        Vector3 dir = end - start;
        float distance = dir.magnitude;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = fromObj.transform.forward;
        }
        else
        {
            dir = dir.normalized;
        }

        // 최소 길이 보정 (체인은 더 길게)
        float minLength = 2f;

        if (distance < minLength)
        {
            Vector3 center = (start + end) * 0.5f;

            start = center - dir * (minLength * 0.5f);
            end = center + dir * (minLength * 0.5f);

            distance = minLength;
        }

        // 이펙트 생성, 오디오 재생
        GameObject fx = Instantiate(prefab);
        AudioManager.Instance.PlaySfx(clip);
        LineRenderer lr = fx.GetComponent<LineRenderer>();

        if (lr != null)
        {
            lr.useWorldSpace = true;

            int pointCount = 5; // 체인 포인트
            lr.positionCount = pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector3 position = Vector3.Lerp(start, end, t);

                // 체인은 더 크게 흔들림
                if (i != 0 && i != pointCount - 1)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.4f;
                    position += new Vector3(offset.x, 0f, offset.y);
                }

                lr.SetPosition(i, position);
            }
        }

        Destroy(fx, 0.6f);
    }
}
