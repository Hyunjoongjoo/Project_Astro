using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    private NetworkPrefabRef _myPrefab;
    private float _finalCooldown;
    private PlayerRef _ownerPlayer;

    public float FinalCooldown => _finalCooldown;
    public DeployState DeployState { get; private set; }
    public CastingState CastState { get; private set; }
    public ISkill CurUniqueSkill => curUniqueSkill;
    public float RespawnTime => _respawnTime;

    //아이템용 옵저버
    [SerializeField] private HeroItemObserver _itemObserver;

    //이번 스폰/갱신 때 이미 적용한 스킬 증강 ID를 기억해서 중복 덮어쓰기 방지
    private HashSet<string> _appliedAugments = new HashSet<string>();


    public override void Spawned()
    {
        BaseUnitInit();

        unitType = UnitType.Hero;

        normalAttack = _normalAttackData.CreateInstance(this);
        curUniqueSkill = _standardSkillData.CreateInstance(this);

        _stageManager = FindFirstObjectByType<StageManager>();

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
        //_respawnTime = _unitStat.RespawnTime.Value;
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
        ApplyEquippedItems();
        _finalCooldown = GetFinalRespawnCooldown();
        HeroSpawner.Instance.StartSummonCooldown(_ownerPlayer, _myPrefab, _finalCooldown);
        Debug.Log($"[쿨 시작] owner:{_ownerPlayer.PlayerId}, finalCooldown:{_finalCooldown}");
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
    public void Setup(Team myTeam, Vector3 targetPos, float deployDelay, NetworkPrefabRef prefab, PlayerRef owner)
    {
        _targetPos = targetPos;
        _deployDelay = deployDelay;
        _myPrefab = prefab;
        _ownerPlayer = owner;

        Setup(myTeam);

        HeroStatData statData = HeroManager.Instance.GetStatus(unitId);
        _respawnTime = statData.spawnCooldown;
        //float reduction = statData.cooltimeReduce;
        //_finalCooldown = Mathf.Max(_respawnTime * (1f - reduction), 0.1f);
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
    //3.18 수정/ 콘피그테이블을 선택 시 체크하도록 변경
    private void ApplyAugments(PlayerNetworkData data)
    {

        for (int i = 0; i < SlotData_5.Length; i++)
        {
            string rawId = data.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();
            if (string.IsNullOrEmpty(rawId))
                continue;

            //중복 방지
            if (_appliedAugments.Contains(rawId))
                continue;

            //꼬리표 분리
            string[] parts = rawId.Split('#');
            string baseId = parts[0];
            int tierIndex = 0;
            if (parts.Length > 1) int.TryParse(parts[1], out tierIndex);

            SkillAugmentSO so = AugmentController.Instance.GetSkillAugmentById(baseId);
            if (so == null)
                continue;

            if (so.TargetHeroID != unitId)
                continue;

            if (tierIndex >= so.Tiers.Length)
                continue;

            BaseSkillSO newSkill = so.Tiers[tierIndex].CombatSkillData;

            if (newSkill == null)
                continue;

            Debug.Log($"[팀 공유 스킬 증강 적용] 영웅:{unitId} <- 증강:{baseId} (Tier: {tierIndex})");

            //적용된 증강 기록 및 실제 스킬 교체
            _appliedAugments.Add(rawId);
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

    public float GetFinalRespawnCooldown()
    {
        // 기본 쿨
        float baseCooldown = _unitStat.RespawnTime.Value;

        // 쿨감
        float cooldownReduction = _unitStat.CooldownReduction.Value;

        // 계산
        float finalCooldown = baseCooldown * (1f - cooldownReduction);

        return Mathf.Max(finalCooldown, 0.1f);
    }

    private void ApplyEquippedItems()
    {
        if (!Object.HasStateAuthority) return;
        Debug.Log($"[PlayerDataMap] contains owner:{_ownerPlayer.PlayerId} = {_stageManager.PlayerDataMap.ContainsKey(_ownerPlayer)}");
        //자신의 영웅 슬롯 인덱스 찾기
        var playerData = _stageManager.PlayerDataMap.Get(_ownerPlayer);
        int myHeroIndex = -1;
        Debug.Log($"[아이템 적용] owner:{_ownerPlayer.PlayerId}");
        for (int i = 0; i < SlotData_5.Length; i++)
        {
            string ownedId = playerData.OwnedHeroes.Get(i).Replace("\0", "").Trim();
            if (ownedId == unitId)
            {
                myHeroIndex = i;
                break;
            }
        }

        //덱에 없는 유닛이면 무시
        if (myHeroIndex == -1) return;

        //장착된 아이템 ID 추출 (인덱스 * 2, 인덱스 * 2 + 1)
        int slotA = myHeroIndex * 2;
        int slotB = myHeroIndex * 2 + 1;

        string itemA = playerData.HeroEquippedItems.Get(slotA).Replace("\0", "").Trim();
        string itemB = playerData.HeroEquippedItems.Get(slotB).Replace("\0", "").Trim();

        List<string> equippedItemIds = new List<string>();
        if (!string.IsNullOrEmpty(itemA)) equippedItemIds.Add(itemA);
        if (!string.IsNullOrEmpty(itemB)) equippedItemIds.Add(itemB);

        if (equippedItemIds.Count == 0) return; //낀 아이템이 없으면 패스

        //ItemEffectData 수집
        List<ItemEffectData> totalEffects = new List<ItemEffectData>();
        var allEffects = TableManager.Instance.ItemEffectTable.GetAll();

        foreach (string itemId in equippedItemIds)
        {
            var itemData = TableManager.Instance.ItemTable.Get(itemId);
            if (itemData != null)
            {
                //EffectGroupId와 일치하는 효과들 전부 적ㅇ용
                for (int i = 0; i < allEffects.Count; i++)
                {
                    if (allEffects[i].effectGroupId == itemData.effectGroupId)
                    {
                        totalEffects.Add(allEffects[i]);
                    }
                }
            }
        }

        //옵저버에 데이터 주입하고 업데이트 시작
        if (totalEffects.Count > 0)
        {
            if (_itemObserver != null)
            {
                _itemObserver.Init(this, _unitStat, totalEffects);
                _itemObserver.enabled = true;
                Debug.Log($"영웅 {unitId}에 {totalEffects.Count}개의 아이템 효과 부착완료");
            }
            else
            {
                Debug.LogWarning($"{unitId} 프리팹 확인 필요,  HeroItemObserver 컴포넌트");
            }
        }
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

