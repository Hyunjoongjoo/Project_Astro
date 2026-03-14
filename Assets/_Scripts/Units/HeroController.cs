using Fusion;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class HeroController : UnitController
{
    // === 미니언 속성에 추가로 영웅만이 가지는 필드 ===

    [Header("기본 스킬 (증강 미적용)")]
    [SerializeField] private BaseSkillSO _standardSkillData;

    private float _respawnTime;
    public ISkill curUniqueSkill;
    private Vector3 _targetPos;
    private float _deployDelay;
    private StageManager _stageManager;

    public DeployState DeployState { get; private set; }
    public CastingState CastState { get; private set; }
    public ISkill CurUniqueSkill => curUniqueSkill;
    public float RespawnTime => _respawnTime;

    //이번 스폰/갱신 때 이미 적용한 스킬 증강 ID를 기억해서 중복 덮어쓰기 방지
    private HashSet<string> _appliedAugments = new HashSet<string>();

    public override void Spawned()
    {
        BaseUnitInit();

        unitType = UnitType.Hero;

        normalAttack = _normalAttackData.CreateInstance(this);
        curUniqueSkill = _standardSkillData.CreateInstance(this);

        _stageManager = FindFirstObjectByType<StageManager>();

        HeroAnimator = GetComponent<Animator>();
        // 부스터는 반드시 자식 오브젝트에서 첫번째에 위치한다.
        BoosterAnimator = transform.GetChild(0).GetComponent<Animator>();

        if (!Object.HasStateAuthority) return;
        // === 이 아래론 마스터 클라이언트가 아니면 실행되지 않음. ===

        RefreshAugments();

        // 상태 인스턴스 생성
        StateMachine = new StateMachine();
        DeployState = new DeployState(this);
        DetectState = new DetectState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
        CastState = new CastingState(this);
        DieState = new DieState(this);

        _unitStat = GetComponent<UnitStat>();

        HeroStatData statData = HeroManager.Instance.GetStatus(unitId);

        //UnitStat 초기화
        _unitStat.Init(statData);

        _unitStat.OnStatChanged += RefreshStatRuntime;//이벤트 구독

        //Stat 기반 값 적용
        MaxHealth = _unitStat.MaxHp.Value;
        CurrentHealth = MaxHealth;
        _respawnTime = _unitStat.RespawnTime.Value;
        agent.speed = MoveSpeed;

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
        curUniqueSkill.Initialize();
        DeployState.SetDeployData(_targetPos, _deployDelay);
        StateMachine.ChangeState(DeployState);
    }

    private void OnDestroy()
    {
        if (_unitStat != null)
        {
            _unitStat.OnStatChanged -= RefreshStatRuntime;//이벤트 해제
        }
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

        if (curUniqueSkill is ShieldSkill shield)
        {
            shield.Tick();
        }
    }

    // --- 생성시 초기화 관련 메서드 ---

    // 스폰 전에 실행되는 메서드
    public void Setup(Team myTeam, Vector3 targetPos, float deployDelay)
    {
        _targetPos = targetPos;
        _deployDelay = deployDelay;

        Setup(myTeam);
    }

    // 스킬 타입에 맞는 VFX 프리팹 반환
    public GameObject GetSkillVFX(SkillType type)
    {
        if (curUniqueSkill != null && curUniqueSkill.Data.skillType == type)
            return curUniqueSkill.Data.skillVFX;

        if (_standardSkillData != null && _standardSkillData.skillType == type)
            return _standardSkillData.skillVFX;

        return null;
    }

    // 스킬 증강 시 스킬 교체(실제 교체)
    private void ChangeSkill(BaseSkillSO newSkillSO)
    {
        if (newSkillSO != null)
        {
            curUniqueSkill = newSkillSO.CreateInstance(this);
            Debug.Log($"[{unitId}] 스킬이 {newSkillSO.name}으로 교체되었습니다!");
        }
    }

    //3.11 리팩토링
    //콘피그테이블 참조하도록 변경, 팀원 증강 중복방지 
    private void ApplyAugments(PlayerNetworkData data)
    {
        //Config 테이블 참조
        int reinforceNum = 6; 
        var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
        if (config != null) reinforceNum = int.Parse(config.configValue);

        for (int i = 0; i < SlotData_5.Length; i++)
        {
            string augmentId = data.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();
            if (string.IsNullOrEmpty(augmentId))
                continue;

            //중복 방지
            if (_appliedAugments.Contains(augmentId))
                continue;

            SkillAugmentSO so = AugmentController.Instance.GetSkillAugmentById(augmentId);
            if (so == null)
                continue;

            if (so.TargetHeroID != unitId)
                continue;

            int tierIndex = data.TotalAugmentPicks >= reinforceNum ? 1 : 0;

            if (tierIndex >= so.Tiers.Length)
                continue;

            BaseSkillSO newSkill = so.Tiers[tierIndex].CombatSkillData;

            if (newSkill == null)
                continue;

            Debug.Log($"[팀 공유 스킬 증강 적용] 영웅:{unitId} <- 증강:{augmentId} (Tier: {tierIndex})");

            //적용된 증강 기록 및 실제 스킬 교체
            _appliedAugments.Add(augmentId);
            ChangeSkill(newSkill);
        }
    }

    //외부에서 스킬 증강을 갱신할 것이라면...
    public void RefreshAugments()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        _appliedAugments.Clear();

        foreach (var player in _stageManager.PlayerDataMap)
        {
            if (player.Value.Team != team)
                continue;

            ApplyAugments(player.Value);
        }
    }

    //Stat 변경 시 NavMesh 갱신용 메서드 추가(이미 배치된 유닛의 이동속도 변경 시)
    public void RefreshStatRuntime()
    {
        if (agent != null)
        {
            agent.speed = MoveSpeed;
        }
    }

    public override void Render()
    {
        BoosterAnimator.SetBool("isActive", BoosterRender);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();//기존 기즈모

        if (curUniqueSkill == null)
            return;

        if (curUniqueSkill.Data is ShieldSkillSO shieldData)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, shieldData.aoeRange);
        }
    }
#endif
}

