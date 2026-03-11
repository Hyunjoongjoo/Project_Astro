using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//증강 시스템의 전체 흐름을 통제하는 네트워크 컨트롤러
//경험치 감지 => 덱 픽업 => 서버 검증

//3.9 타이머 루프 추가 => 상태 Playing 일 시 저장된 플레이어별 증강 타이머 감소시키기
//타이머 제어용 RPC 추가(타이머시작, 시간 끝나고 강제선택 2개)
//증강 창이 열릴 때 타이머를 시작하고, 선택이 승인되면 타이머를 정지
public class AugmentController : NetworkBehaviour
{
    public static AugmentController Instance { get; private set; }

    //카드 생성용 매니저
    private AugmentDeckManager _deckManager;

    //유저 인게임 상태와 게이지 관리하는 스테이지 매니저
    private StageManager _stageManager;

    //스킬증강 SO 할당
    //추후 리소스로드 or 어드레서블로 관리
    [Header("스킬 증강 데이터베이스")]
    [SerializeField] private List<SkillAugmentSO> _allSkillAugments;

    //영웅들의 기본 스킬 SO들을 담아둘 리스트 추가
    [Header("기본 스킬 데이터베이스 (영웅 카드용)")]
    [SerializeField] private List<BaseSkillSO> _allBaseSkills;

    //캐싱용
    private AugmentData _localSelectedData;
    //2vs2 분할 배송을 위한 클라이언트 임시 보관소
    private List<AugmentData> _tempMyCards;


    //인스펙터 직렬화 해둘 히어로 아이콘SO
    [SerializeField] private HeroIconDataSO _heroIconSO;

    //인게임 개별 타이머용 네트워크 변수 선언
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, float> PlayerAugmentTimers => default;
    [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, NetworkBool> IsPlayerAugmenting => default;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //오브젝트 스폰될 때 딱 한 번 호출
    public override void Spawned()
    {
        //할당된 SO 데이터를 바탕으로 DeckManager 가동
        _deckManager = new AugmentDeckManager(_allSkillAugments, _heroIconSO, _allBaseSkills);
        //현재 씬에 있는 스테이지 매니저 찾아서 캐싱
        _stageManager = FindFirstObjectByType<StageManager>();
    }

    //3.9
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || _stageManager == null) return;

        //인게임 상태일 때만 개별 플레이어 증강 타이머 연산
        if (_stageManager.CurrentState == StageState.Playing)
        {
            foreach (var kvp in IsPlayerAugmenting)
            {
                if (kvp.Value == true)
                {
                    PlayerRef pRef = kvp.Key;
                    float timeLeft = PlayerAugmentTimers.Get(pRef) - Runner.DeltaTime;

                    if (timeLeft <= 0)
                    {
                        timeLeft = 0;
                        IsPlayerAugmenting.Set(pRef, false); //타이머 정지
                        RPC_ForcePlayerAugmentTimeout(pRef); //타임아웃 명령
                    }

                    PlayerAugmentTimers.Set(pRef, timeLeft);
                }
            }
        }
    }

    //HeroController에서 사용할 증강 SO 검색 함수26-03-03
    public SkillAugmentSO GetSkillAugmentById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (var so in _allSkillAugments)
        {
            if (so != null && so.AugmentID == id)
            {
                return so;
            }
        }

        return null;
    }


    //카드 3장 뽑아서 화면에 띄우기
    //플레이어 상태 분석해서 맞춤 카드 3장 띄우기
    //UI에서 증강선택 토글 버튼 눌렀을 때 호출

    //3.9 서버가 팀원 데이터를 모아 덱 매니저 돌리고 클라로 배달
    public void OpenAugmentWindow()
    {
        if (_stageManager == null)
        {
            _stageManager = FindFirstObjectByType<StageManager>();
        }

        //마스터만 카드를 뽑아 중복 차단
        if (!Object.HasStateAuthority) return;

        Debug.Log("데이터 분석");


        //Config테이블에서 강화 달성 횟수 가져오기
        int reinforceNum = 6; //파싱 실패 시 기본값
        var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
        if (config != null) reinforceNum = int.Parse(config.configValue);

        //블루팀, 레드팀으로 플레이어 분류
        Dictionary<Team, List<PlayerRef>> teamPlayers = new Dictionary<Team, List<PlayerRef>>();
        teamPlayers[Team.Blue] = new List<PlayerRef>();
        teamPlayers[Team.Red] = new List<PlayerRef>();

        foreach (var kvp in _stageManager.PlayerDataMap)
        {
            if (kvp.Value.Team == Team.Blue || kvp.Value.Team == Team.Red)
            {
                teamPlayers[kvp.Value.Team].Add(kvp.Key);
            }
        }


        //팀 단위로 겹침 방지 처리하며 카드 생성
        foreach (var team in teamPlayers.Values)
        {
            List<string> excludedSkillTargets = new List<string>();
            List<string> teamHeroes = new List<string>();

            //해당 팀의 모든 영웅 데이터 취합
            foreach (var member in team)
            {
                var data = _stageManager.PlayerDataMap.Get(member);
                for (int i = 0; i < SlotData_5.Length; i++)
                {
                    string heroId = data.OwnedHeroes.Get(i).Replace("\0", "").Trim();
                    if (!string.IsNullOrEmpty(heroId)) teamHeroes.Add(heroId);
                }
            }

            //카드 뽑고 딕셔너리에 저장해두고 크로스 배송
            Dictionary<PlayerRef, List<AugmentData>> generatedCards = new Dictionary<PlayerRef, List<AugmentData>>();

            //유저별 카드 생성 및 배달
            foreach (var player in team)
            {
                var myData = _stageManager.PlayerDataMap.Get(player);

                List<string> myHeroes = new List<string>();
                for (int i = 0; i < SlotData_5.Length; i++)
                {
                    string heroId = myData.OwnedHeroes.Get(i).Replace("\0", "").Trim();
                    if (!string.IsNullOrEmpty(heroId)) myHeroes.Add(heroId);
                }

                List<string> mySkills = new List<string>();
                for (int i = 0; i < SlotData_5.Length; i++)
                {
                    string skillId = myData.OwnedSkillAugments.Get(i).Replace("\0", "").Trim();
                    if (!string.IsNullOrEmpty(skillId)) mySkills.Add(skillId);
                }

                bool isItemFull = true;
                for (int i = 0; i < SlotData_3.Length; i++)
                {
                    string itemId = myData.InventoryItems.Get(i).Replace("\0", "").Trim();
                    if (string.IsNullOrEmpty(itemId)) { isItemFull = false; break; }
                }

                List<AugmentData> cards = _deckManager.GenerateCards(
                    isFirstSelection: myData.TotalAugmentPicks == 0,
                    ownedHeroIds: myHeroes,
                    ownedSkillAugmentIds: mySkills,
                    isItemFull: isItemFull,
                    totalAugmentPicks: myData.TotalAugmentPicks,
                    reinforceNumber: reinforceNum,
                    teamHeroIds: teamHeroes,
                    excludedSkillTargetIds: excludedSkillTargets //겹침 방지 리스트 전달
                );
                //뽑은 카드를 딕셔너리 저장
                generatedCards[player] = cards;
            }
            //팀원이 있는지 확인 후 1:1 또는 2:2 패킷 묶음 발송
            foreach (var targetPlayer in team)
            {
                var myCards = generatedCards[targetPlayer];
                if (myCards.Count < 3) continue;

                PlayerRef teammateRef = default;
                List<AugmentData> teamCards = null;
                string teammateName = "";

                //나와 다른 아군 팀원 찾기 및 닉네임 가져오기
                foreach (var other in team)
                {
                    if (other != targetPlayer)
                    {
                        teammateRef = other;
                        teamCards = generatedCards[other];
                        teammateName = _stageManager.PlayerDataMap.Get(other).PlayerName.ToString();
                        break;
                    }
                }

                if (teamCards != null && teamCards.Count == 3)
                {
                    //내 카드 배송
                    RPC_DeliverMyCards(targetPlayer,
                        myCards[0].targetId, (int)myCards[0].type,
                        myCards[1].targetId, (int)myCards[1].type,
                        myCards[2].targetId, (int)myCards[2].type,
                        true);

                    //아군 카드 후속 배송
                    RPC_DeliverTeamCards(targetPlayer,
                        teamCards[0].targetId, (int)teamCards[0].type,
                        teamCards[1].targetId, (int)teamCards[1].type,
                        teamCards[2].targetId, (int)teamCards[2].type,
                        teammateName);
                }
                else
                {
                    //1vs1: 내 카드 3장만 배달
                    RPC_DeliverMyCards(targetPlayer,
                        myCards[0].targetId, (int)myCards[0].type,
                        myCards[1].targetId, (int)myCards[1].type,
                        myCards[2].targetId, (int)myCards[2].type,
                        false);
                }
            }
        }
    }

    //3.10
    //카드 3장의 ID와 타입만 클라이언트에게 전달
    //받은 클라는 테이블 체크 후 재조립

    //내 카드만 전달받는 RPC(512바이트 통과)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeliverMyCards(PlayerRef target,
        NetworkString<_32> id0, int type0,
        NetworkString<_32> id1, int type1,
        NetworkString<_32> id2, int type2,
        NetworkBool hasTeamCards)
    {
        if (Runner.LocalPlayer == target)
        {
            PlayerNetworkData myData = _stageManager.PlayerDataMap.Get(Runner.LocalPlayer);
            int reinforceNum = 6;
            var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
            if (config != null) reinforceNum = int.Parse(config.configValue);

            // 임시 보관소에 저장
            _tempMyCards = new List<AugmentData>();
            var card0 = RebuildAugmentData(id0.ToString(), (AugmentType)type0, myData, reinforceNum, "MyCard_1");
            if (card0 != null) _tempMyCards.Add(card0);
            var card1 = RebuildAugmentData(id1.ToString(), (AugmentType)type1, myData, reinforceNum, "MyCard_2");
            if (card1 != null) _tempMyCards.Add(card1);
            var card2 = RebuildAugmentData(id2.ToString(), (AugmentType)type2, myData, reinforceNum, "MyCard_3");
            if (card2 != null) _tempMyCards.Add(card2);

            // 1vs1 모드라면 더 기다릴 것 없이 바로 화면에 출력!
            if (!hasTeamCards && _tempMyCards.Count > 0)
            {
                AugmentManager.Instance.ShowAugmentWindow(_tempMyCards);

                if (_stageManager.CurrentState == StageState.Playing)
                {
                    RPC_StartInGameAugmentTimer(Runner.LocalPlayer);
                }
                _tempMyCards = null; // 사용 후 비우기
            }
        }
    }

    //아군 카드만 전달받는 후속 RPC 패킷넘치는 거 방지용
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeliverTeamCards(PlayerRef target,
        NetworkString<_32> id0, int type0,
        NetworkString<_32> id1, int type1,
        NetworkString<_32> id2, int type2,
        NetworkString<_16> teamName)
    {
        if (Runner.LocalPlayer == target)
        {
            PlayerNetworkData myData = _stageManager.PlayerDataMap.Get(Runner.LocalPlayer); 
            int reinforceNum = 6;
            var config = TableManager.Instance.ConfigTable.Get("augment_reinforce_number");
            if (config != null) reinforceNum = int.Parse(config.configValue);

            List<AugmentData> teamCards = new List<AugmentData>();
            var card0 = RebuildAugmentData(id0.ToString(), (AugmentType)type0, myData, reinforceNum, "TeamCard_1");
            if (card0 != null) teamCards.Add(card0);
            var card1 = RebuildAugmentData(id1.ToString(), (AugmentType)type1, myData, reinforceNum, "TeamCard_2");
            if (card1 != null) teamCards.Add(card1);
            var card2 = RebuildAugmentData(id2.ToString(), (AugmentType)type2, myData, reinforceNum, "TeamCard_3");
            if (card2 != null) teamCards.Add(card2);

            //미리 도착해 있던 내 카드와 방금 도착한 아군 카드를 합쳐서 UI에 넘겨줌
            if (_tempMyCards != null && _tempMyCards.Count > 0)
            {
                AugmentManager.Instance.ShowAugmentWindow(_tempMyCards, teamCards, teamName.ToString());

                if (_stageManager.CurrentState == StageState.Playing)
                {
                    RPC_StartInGameAugmentTimer(Runner.LocalPlayer);
                }
                _tempMyCards = null; //사용 후 비우기
            }
        }
    }

    private AugmentData RebuildAugmentData(string id, AugmentType type, PlayerNetworkData myData, int reinforceNum, string instanceId)
    {
        if (string.IsNullOrEmpty(id)) return null;

        AugmentData data = new AugmentData();
        data.instanceId = instanceId;
        data.type = type;
        data.targetId = id;

        if (type == AugmentType.Hero)
        {
            var heroData = TableManager.Instance.HeroTable.Get(id);
            if (heroData != null)
            {
                data.titleName = TableManager.Instance.GetString(heroData.heroName);
                data.description = TableManager.Instance.GetString(heroData.heroDesc);
                data.heroType = heroData.heroType;
                data.heroRole = heroData.heroRole;

                if (_heroIconSO != null)
                {
                    data.mainIcon = _heroIconSO.GetIcon(id);
                    data.heroPrefab = _heroIconSO.GetPrefab(id);
                }

                var heroStat = TableManager.Instance.HeroStatTable.Get(heroData.heroStatId);
                if (heroStat != null)
                {
                    data.baseSpawnCooldown = heroStat.spawnCooldown;
                    data.currentSpawnCooldown = heroStat.spawnCooldown;
                    data.moveType = heroStat.moveType;
                }

                if (_allBaseSkills != null)
                {
                    var baseSkill = _allBaseSkills.FirstOrDefault(s => s.heroId == id && s.skillType == SkillType.NormalSkill);
                    if (baseSkill != null) data.skillData = baseSkill;
                }
            }
        }
        else if (type == AugmentType.Skill)
        {
            var skill = GetSkillAugmentById(id);
            if (skill != null)
            {
                int tierIndex = (myData.TotalAugmentPicks >= reinforceNum) ? 1 : 0;

                data.titleName = TableManager.Instance.GetString(skill.TitleStringID);
                data.description = TableManager.Instance.GetString(skill.Tiers[tierIndex].DescStringID);
                data.mainIcon = skill.Icon;
                data.skillData = skill.Tiers[tierIndex].CombatSkillData;

                var heroData = TableManager.Instance.HeroTable.Get(skill.TargetHeroID);
                if (heroData != null) data.targetHeroName = TableManager.Instance.GetString(heroData.heroName);

                if (_heroIconSO != null) data.targetHeroIcon = _heroIconSO.GetIcon(skill.TargetHeroID);

                if (_allBaseSkills != null)
                {
                    var baseSkill = _allBaseSkills.FirstOrDefault(s => s.heroId == skill.TargetHeroID && s.skillType == SkillType.NormalSkill);
                    if (baseSkill != null) data.baseSkillName = baseSkill.skillName;
                }
            }
        }
        else if (type == AugmentType.Item)
        {
            var itemData = TableManager.Instance.ItemTable.Get(id);
            if (itemData != null)
            {
                data.titleName = TableManager.Instance.GetString(itemData.name);
                data.description = "";
            }
        }

        return data;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartInGameAugmentTimer(PlayerRef player)
    {
        float selectTime = 15f;
        var config = TableManager.Instance.ConfigTable.Get("augment_select_time");
        if (config != null) selectTime = float.Parse(config.configValue);

        PlayerAugmentTimers.Set(player, selectTime);
        IsPlayerAugmenting.Set(player, true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ForcePlayerAugmentTimeout(PlayerRef target)
    {
        if (Runner.LocalPlayer == target)
        {
            Debug.Log("증강 선택 제한 시간 종료, 자동 선택 실행");
            AugmentManager.Instance.ForceRandomPick();
        }
    }


    //유저의 카드 선택 및 서버에 결재 올리기
    public void SelectAugment(AugmentData data)
    {
        //서버에 요청하기
        _localSelectedData = data;//카드기억
        RPC_RequestSelectAugment(data.targetId, data.type);
    }

    //클라 => 서버로 증강 선택 요청하기
    //꽉찼나 체크
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSelectAugment(string targetId, AugmentType type, RpcInfo info = default)
    {
        //플레이어찾기
        PlayerNetworkData data = _stageManager.PlayerDataMap.Get(info.Source);
        bool isValid = true;

        //패킷이 날아오는 동안 슬롯이 꽉 찼는지 다시 한번 확인
        if (type == AugmentType.Hero && IsSlotFull5(data.OwnedHeroes)) isValid = false;
        if (type == AugmentType.Item && IsSlotFull3(data.InventoryItems)) isValid = false;
        if (type == AugmentType.Skill && IsSlotFull5(data.OwnedSkillAugments)) isValid = false;

        //검증 통과 여부에 따라 컨펌 or 리젝트
        if (isValid)
        {
            RPC_ConfirmAugment(targetId, type, info.Source);
        }
        else
        {
            RPC_RejectAugment(info.Source);
        }
    }

    //통과
    //3.10 무료 증강 예외처리 삭제 & 이중 차감 방지 로직 구현
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ConfirmAugment(string targetId, AugmentType type, PlayerRef player)
    {
        //마스터 권한인 경우엔 PlayerNetworkData 업데이트 및 게이지 차감 처리
        if (Object.HasStateAuthority)
        {
            if (_stageManager.PlayerDataMap.TryGet(player, out PlayerNetworkData data))
            {
                Team myTeam = data.Team;

                //카드 확정 전에 우리 팀의 현재 최대 증강 횟수 파악
                int currentTeamMaxPicks = 0;
                foreach (var kvp in _stageManager.PlayerDataMap)
                {
                    if (kvp.Value.Team == myTeam && kvp.Value.TotalAugmentPicks > currentTeamMaxPicks)
                    {
                        currentTeamMaxPicks = kvp.Value.TotalAugmentPicks;
                    }
                }

                //실제 데이터 적용
                AugmentExecutor.ApplyAugment(_stageManager, player, type, targetId);

                var updatedData = _stageManager.PlayerDataMap.Get(player);

                //내 새로운 픽 횟수가 팀의 기존 최대 픽 횟수보다 크다면 얘가 결제
                if (updatedData.TotalAugmentPicks > currentTeamMaxPicks)
                {
                    //Config 테이블에서 비용 가져오기 (기본값 120)
                    int cost = 120;
                    var config = TableManager.Instance.ConfigTable.Get("augment_gauge");
                    if (config != null) cost = int.Parse(config.configValue);

                    _stageManager.DecreaseAugmentGauge(myTeam, cost);
                }
                else
                {
                    Debug.Log($"[{myTeam}] 아군이 이미 게이지 소모해서 패스");
                }

                //인게임 타이머 초기화 처리
                if (_stageManager.CurrentState == StageState.Playing)
                {
                    IsPlayerAugmenting.Set(player, false);
                    PlayerAugmentTimers.Set(player, 0f);
                }
            }
            //나를 포함한 모든 유저에게 갱신 알림을 보냄
            RPC_NotifyTeammateRefresh(player);
        }
        //카드를 산 사람이 로컬이 맞다면, UI를 갱신
        if (player == Runner.LocalPlayer)
        {
            if (type == AugmentType.Hero)
            {
                if (_localSelectedData != null && _localSelectedData.targetId == targetId)
                {
                    //조립된 데이터를 UI 매니저에게 넘겨서 하단 덱에 카드 슬롯을 추가
                    AugmentManager.Instance.AddHeroCard(_localSelectedData);
                    _localSelectedData = null; // 다 썼으니 비워줌
                }
            }
            //전투 시작 전이면 카드 다 골랐다고 스테이지에 보고
            if (_stageManager.CurrentState == StageState.PreGameAugment)
            {
                _stageManager.RPC_ReportAugmentComplete(player);
            }
        }
    }

    //UI갱신 신호 쏘기
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyTeammateRefresh(PlayerRef ownerPlayer)
    {
        if (Runner.LocalPlayer != ownerPlayer)
        {
            if (_stageManager.PlayerDataMap.TryGet(ownerPlayer, out var latestData))
            {
                _stageManager.UpdateTeammateUI(ownerPlayer, latestData.OwnedHeroes);
            }
        }
    }

    //반려
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RejectAugment(PlayerRef player)
    {
        Debug.LogWarning("슬롯이 가득 차서 서버에서 장착 반려");
    }


    //네트워크배열 대신 슬롯체크용
    //5칸
    private bool IsSlotFull5(SlotData_5 slotData)
    {
        for (int i = 0; i < SlotData_5.Length; i++)
        {
            if (string.IsNullOrEmpty(slotData.Get(i).Replace("\0", "").Trim())) return false;
        }
        return true;
    }

    //3칸
    private bool IsSlotFull3(SlotData_3 slotData)
    {
        for (int i = 0; i < SlotData_3.Length; i++)
        {
            if (string.IsNullOrEmpty(slotData.Get(i).Replace("\0", "").Trim())) return false;
        }
        return true;
    }
}
