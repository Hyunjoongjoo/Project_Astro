using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class HeroController : UnitController
{
    // === 미니언 속성에 추가로 영웅만이 가지는 필드 ===

    [Header("기본 스킬 (증강 미적용)")]
    [SerializeField] private BaseSkillSO _standardSkillData;
    [Header("증강 A타입, 강화형")]
    [SerializeField] private BaseSkillSO _typeASkillData;
    [SerializeField] private BaseSkillSO _typeAEnhanceSkillData;
    [Header("증강 B타입, 강화형")]
    [SerializeField] private BaseSkillSO _typeBSkillData;
    [SerializeField] private BaseSkillSO _typeBEnhanceSkillData;

    private float _respawnTime;
    public ISkill curUniqueSkill;
    private Vector3 _targetPos;
    private float _deployDelay;

    public DeployState DeployState { get; private set; }
    public CastingState CastState { get; private set; }
    public ISkill CurUniqueSkill => curUniqueSkill;

    public float RespawnTime => _respawnTime;
    public float HealPower => _unitStat.HealPower.Value;

    public override void Spawned()
    {
        BaseUnitInit();

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

        HeroStatData statData = HeroManager.Instance.GetStatus(_unitId);

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
        _targetPos = targetPos;
        _deployDelay = deployDelay;

        Setup(myTeam);
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

            if (so.TargetHeroID != _unitId)
                continue;

            int tierIndex = data.TotalAugmentPicks >= 6 ? 1 : 0;

            if (tierIndex >= so.Tiers.Length)
                continue;

            SkillDataSO newSkill = so.Tiers[tierIndex].CombatSkillData;

            if (newSkill == null)
                continue;

            // ChangeSkill(newSkill);
        }
    }
}
