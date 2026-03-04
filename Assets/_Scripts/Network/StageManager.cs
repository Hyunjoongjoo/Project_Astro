using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum StageState
{
    WaitingForPlayers,    // 플레이어 대기 중
    AssigningTeams,       // 팀 배정 중
    ShowingPlayerInfo,    // 플레이어 정보 표시
    AugmentSelection,     // 증강 선택 단계
    Countdown,            // 카운트다운
    Playing,              // 게임 진행 중
    GameOver              // 게임 종료
}

public struct RewardData
{
    public int UserExp;
    public int HeroExp;
    public int GoldAmount;
}

public struct HeroResultData
{
    public string Name;        
    public string IconPath;    
    public int    Level;          
    public int    CurrentExp;     
    public int    AddedExp;       
    public int    MaxExp;         
}

public class StageManager : NetworkBehaviour
{
    [Networked, HideInInspector] public StageState CurrentState { get; set; }
    [Networked, HideInInspector] public float StateTimer { get; set; }
    [Networked, HideInInspector] public int CountdownValue { get; set; }

    // 플레이어별 팀 정보 (PlayerRef를 키로 사용)
    //[Networked, Capacity(4)]
    //public NetworkDictionary<PlayerRef, Team> PlayerTeams => default;

    // 구조체로 퓨전 접속 시 필요한 플레이어 정보 담기.
    // 이게 되면 위에 플레이어 팀 딕셔너리 제거.
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, PlayerNetworkData> PlayerDataMap => default;

    //플레이어별 증강 선택 완료 여부 추적용
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, NetworkBool> _playerAugmentReady => default;

    [Networked, Capacity(2)]
    public NetworkDictionary<Team, int> AugmentExp => default;

    [SerializeField] private NetworkPrefabRef _minionSpawnerPrefab;

    private UserDataManager _userDataManager;

    ObjectContainer _objectContainer;
    private StageUI _stageUI;
    private Camera _mainCamera;

    private MatchType _curMatchType;
    private int _requiredPlayerCount;

    private readonly int GAME_DURATION = 240;
    private readonly float PLAYER_INFO_DURATION = 4f;
    private readonly float COUNTDOWN_INTERVAL = 1f;

    private PlayerNetworkData _localPlayerMap = default;

    private readonly RewardData _winReward = new RewardData { UserExp = 10, HeroExp = 10, GoldAmount = 1000 };
    private readonly RewardData _drawReward = new RewardData { UserExp = 9, HeroExp = 9, GoldAmount = 800 };
    private readonly RewardData _loseReward = new RewardData { UserExp = 8, HeroExp = 8, GoldAmount = 500 };

    private void Awake()
    {
        _mainCamera = Camera.main;
        _stageUI = FindFirstObjectByType<StageUI>();
        if (_stageUI == null)
            Debug.Log("스테이지 UI 찾지 못함");

        _userDataManager = FindFirstObjectByType<UserDataManager>();
        if (_userDataManager == null)
            Debug.Log("UserDataManager 찾지 못함");
    }

    public void Initialize(MatchType matchType, int requiredPlayerCount)
    {
        _curMatchType = matchType;
        _requiredPlayerCount = requiredPlayerCount;
    }

    public override void Spawned()
    {
        GameManager.Instance.ChangeState(GameState.Ready);
        _objectContainer = ObjectContainer.Instance;
        _objectContainer.OnIncreasedAugmentGauge += IncreaseAugmentGauge;

        // 권한 확인. PhotonView.IsMine과 비슷한 쓰임
        // 즉, 이전에 이 StageManager를 스폰한 애가 마스터 클라이언트니까
        // 마스터 클라이언트만 이 조건을 통과함.
        // 마스터 클라이언트가 변수 변경 -> Networked 속성으로 모두에게 동기화
        if (Object.HasStateAuthority)
        {
            CurrentState = StageState.WaitingForPlayers;
        }

        // 각 로컬 유저들은 자신의 닉네임을 가져와서 마스터에게 쏴준다.
        string myNickname = UserDataManager.Instance.ProfileModel.nickName;
        RPC_SubmitPlayerData(Runner.LocalPlayer, myNickname);
    }

    // 마스터는 로컬에서 수신받은 닉네임으로 구조체를 만든다.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitPlayerData(PlayerRef playerRef, NetworkString<_16> nickName)
    {
        var data = new PlayerNetworkData
        {
            PlayerName = nickName,
            Team = Team.None,
            UsedHeroBitmask = 0
        };

        PlayerDataMap.Set(playerRef, data);
        Debug.Log($"플레이어 데이터 수신 : {nickName} ({PlayerDataMap.Count}/{_requiredPlayerCount})");
    }

    // 사용한 영웅 비트값으로 체크하는 메서드임!
    public void MarkHeroUsed(PlayerRef player, int heroId)
    {
        if (Object.HasStateAuthority && PlayerDataMap.TryGet(player, out var data))
        {
            // heroId는 CSV 상의 인덱스(0~31)라고 가정
            data.UsedHeroBitmask |= (uint)(1 << heroId);
            PlayerDataMap.Set(player, data);
        }
    }

    // 네트워크 틱에 맞춘 Update 메서드임
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority == false)
            return;

        switch (CurrentState)
        {
            case StageState.WaitingForPlayers:
                CheckAllPlayersReady();
                break;

            case StageState.ShowingPlayerInfo:
                UpdatePlayerInfoTimer();
                break;

            case StageState.Countdown:
                UpdateCountdown();
                break;

            case StageState.Playing:
                UpdateStageTimer();
                break;
        }
    }

    // =============== 여기부터 인트로 ~ 게임 시작 직전 ===============

    private void CheckAllPlayersReady()
    {
        if (Runner.ActivePlayers.Count() == _requiredPlayerCount
            && PlayerDataMap.Count == _requiredPlayerCount)
        {
            AssignTeams();
            CurrentState = StageState.ShowingPlayerInfo;
            StateTimer = PLAYER_INFO_DURATION;
        }
    }

    private void AssignTeams()
    {
        // 플레이어들을 리스트로 받고
        var players = Runner.ActivePlayers.ToList();

        // 반으로 나눔 ( 2 -> 1, 4 -> 2)
        int half = players.Count / 2;

        // ============== 구조체로 시도 ==============

        for (int i = 0; i < players.Count; i++)
        {
            Team team = i < half ? Team.Blue : Team.Red;

            // 기존 데이터에 팀만 업데이트
            var data = PlayerDataMap.Get(players[i]);
            data.Team = team;
            PlayerDataMap.Set(players[i], data);
        }

        // ============== 여기까지 ==============

        // ============== 기존 팀 배정 ==============

        // 앞 절반 블루팀, 뒤 절반 레드팀
        //for (int i = 0; i < half; i++)
        //    PlayerTeams.Add(players[i], Team.Blue);

        //for (int i = half; i < players.Count; i++)
        //    PlayerTeams.Add(players[i], Team.Red);

        // ============== 여기까지 ==============

        // 팀 자원인 증강 게이지도 추가함.
        AugmentExp.Add(Team.Blue, 0);
        AugmentExp.Add(Team.Red, 0);

        // RPC로 모든 클라이언트에 팀 배정 알림
        RPC_NotifyTeamAssignment();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyTeamAssignment()
    {
        _localPlayerMap = PlayerDataMap.Get(Runner.LocalPlayer);
        Team myTeam = _localPlayerMap.Team;

        Debug.Log($"팀 배정 완료! 내 팀 {myTeam}");

        GameManager.Instance.SetTeam(myTeam);

        // DB로 부터 받아온 플레이어 정보 중 표시할 것 선정
        ShowPlayerInfo(myTeam);
    }

    private void ShowPlayerInfo(Team myTeam)
    {
        // 플레이어 수와 팀에 따라 각 로컬 초기화
        _stageUI.LocalInitialize(PlayerDataMap.Count, myTeam);

        PlayerNetworkData[] data = new PlayerNetworkData[PlayerDataMap.Count];


        // 배열에 플레이어 정보를 채울건데
        // [나, 적1, 팀원, 적2] 순으로 채워짐.
        // 1:1 이면 [나, 적] 으로 채워짐.
        int index = 1;
        foreach (var player in PlayerDataMap)
        {
            if (player.Key == Runner.LocalPlayer) // 나.
                data[0] = player.Value;
            else if (player.Value.Team == myTeam) // 나는 아닌데 같은 팀 -> 2:2라는 뜻
                data[2] = player.Value;
            else // 나도 아니고 같은 팀도 아님 -> 적이라는 뜻
            {
                data[index] = player.Value;
                index += 2;
            }  
        }

        _stageUI.ShowPlayerInfo(data);
    }

    private void UpdatePlayerInfoTimer()
    {
        StateTimer -= Runner.DeltaTime;

        if (StateTimer <= 0)
        {
            // 플레이어 정보 숨기고 증강 선택 시작
            RPC_HidePlayerInfo();
            CurrentState = StageState.AugmentSelection;
            EnterAugmentSelection();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HidePlayerInfo()
    {
        _stageUI.HidePlayerInfo();
    }

    private void EnterAugmentSelection()
    {
        if (Object.HasStateAuthority)
        {
            // 모든 클라이언트에게 증강 UI를 띄우라고 알림
            RPC_RequestAugmentSelection();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RequestAugmentSelection()
    {
        // 3장의 랜덤 카드를 뽑아 UI를 띄움
        AugmentController.Instance.OpenAugmentWindow();

        Debug.Log("증강 선택 시작!");
    }

    // 플레이어들이 증강을 선택하면 마스터에게 알림.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReportAugmentComplete(PlayerRef player)
    {
        if (!_playerAugmentReady.ContainsKey(player))
        {
            _playerAugmentReady.Add(player, true);
        }

        // 모든 플레이어가 선택을 완료했는지 확인
        if (_playerAugmentReady.Count == Runner.ActivePlayers.Count())
        {
            Debug.Log("모든 플레이어 증강 선택 완료. 게임을 시작합니다.");

            CurrentState = StageState.Countdown;
            CountdownValue = 4;
            StateTimer = COUNTDOWN_INTERVAL;
        }
    }

    private void UpdateCountdown()
    {
        // Runner.DeltaTime = 네트워크 입장에서의 Time.deltaTime 같은 거
        StateTimer -= Runner.DeltaTime;

        if (StateTimer <= 0)
        {
            CountdownValue--;

            if (CountdownValue > 0)
            {
                // 카운트다운 업데이트
                RPC_UpdateCountdown(CountdownValue);
                StateTimer = COUNTDOWN_INTERVAL;
            }
            else
            {
                // 카운트 다운 종료 후 게임 시작
                StartGame();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateCountdown(int value)
    {
        _stageUI.UpdateCountdown(value);
    }

    private void StartGame()
    {
        _objectContainer.blueSideStructure[_objectContainer.BridgeIndex].OnDeath += BridgeDestroyed;
        _objectContainer.redSideStructure[_objectContainer.BridgeIndex].OnDeath += BridgeDestroyed;
        Debug.Log("브릿지 파괴에 메서드 구독 완료");

        RPC_StartGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartGame()
    {
        if (Object.HasStateAuthority)
        {
            // 게임 시작 직전 초기화할 것들 (마스터가 대표로)
            InitializeBeforeStartGame();
        }

        _stageUI.HideCountdown();
        GameManager.Instance.ChangeState(GameState.Play);
        Debug.Log("게임 시작!");
    }

    private void InitializeBeforeStartGame()
    {
        Runner.Spawn(_minionSpawnerPrefab);
        CurrentState = StageState.Playing;
        CountdownValue = GAME_DURATION;
        StateTimer = COUNTDOWN_INTERVAL;
    }

    // =============== 여기부터 게임 시작 후 타이머 가동 ===============

    private void UpdateStageTimer()
    {
        StateTimer -= Runner.DeltaTime;

        if (StateTimer <= 0)
        {
            CountdownValue--;

            if (CountdownValue > 0)
            {
                // 인게임 타이머 업데이트
                RPC_UpdateStageTimer(CountdownValue);
                StateTimer = COUNTDOWN_INTERVAL;
            }
            else
            {
                // 인게임 타이머 4분이 다 됨. 
                // 시간 종료 시 승패 규칙에 따라 승패 RPC
                // TODO : 일단 무승부로 처리. 추후 승패 판정 로직 추가
                RPC_GameOver(Team.None);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateStageTimer(int remainingSeconds)
    {
        _stageUI.UpdateStageTimer(remainingSeconds); // UI 갱신
    }

    private void IncreaseAugmentGauge(Team team, int amount)
    {
        if ( AugmentExp.TryGet(team, out int curExp) )
        {
            int value = AugmentExp.Set(team, curExp + amount);
            RPC_UpdateAugmentGauge(team, value);
        }

        else
            Debug.LogError("증강 게이지 증가 실패");
    }

    public void DecreaseAugmentGauge(Team team, int amount)
    {
        if (AugmentExp.TryGet(team, out int curExp))
        {
            int value = AugmentExp.Set(team, curExp - amount);
            RPC_UpdateAugmentGauge(team, value);
        }

        else
            Debug.LogError("증강 게이지 감소 실패");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateAugmentGauge(Team team, int value)
    {
        // if (class == null) 검사를 구조체로 하기.
        if ( _localPlayerMap.Equals(default(PlayerNetworkData)) )
            _localPlayerMap = PlayerDataMap.Get(Runner.LocalPlayer);

        Team myTeam = _localPlayerMap.Team;
        if (myTeam == team)
            _stageUI.UpdateAugmentGauge(value); // UI 갱신
    }

    // =============== 여기부터 함교 파괴 감지 ~ 게임 종료 후 로비로 복귀까지 ===============
    private void BridgeDestroyed(UnitBase unit)
    {
        Debug.Log("브릿지 파괴 이벤트 메서드에 진입 완료");
        CurrentState = StageState.GameOver;

        _objectContainer.blueSideStructure[_objectContainer.BridgeIndex].OnDeath -= BridgeDestroyed;
        _objectContainer.redSideStructure[_objectContainer.BridgeIndex].OnDeath -= BridgeDestroyed;
        _objectContainer.OnIncreasedAugmentGauge -= IncreaseAugmentGauge;
        Debug.Log("각종 이벤트 구독 제거 완료");

        // 브릿지가 파괴된 팀의 반대 팀이 승리 팀
        Team victory = unit.team == Team.Blue ? Team.Red : Team.Blue;

        RPC_GameOver(victory);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameOver(Team victory)
    {
        Debug.Log("게임오버 RPC 진입 성공");
        _stageUI.gameObject.SetActive(true);
        _stageUI.goLobbyBtn.onClick.AddListener(ShutDownAndSceneChange);

        if (_localPlayerMap.Equals(default(PlayerNetworkData)))
            _localPlayerMap = PlayerDataMap.Get(Runner.LocalPlayer);

        Team myTeam = _localPlayerMap.Team;
        Debug.Log($"승리팀 : {victory}, 내 팀 : {myTeam}");

        bool isWin = (myTeam == victory);
        bool isDraw = (victory == Team.None);

        // 승패 결과에 따른 리워드 세팅
        RewardData reward = (victory == Team.None) ? _drawReward : (myTeam == victory ? _winReward : _loseReward);

        // DB에 업데이트
        _ = _userDataManager.UpdateWallet(reward.GoldAmount);
        _ = _userDataManager.UpdateUserDb(reward.UserExp);

        if (victory != Team.None)
        {
            _ = _userDataManager.UpdateRecord(isWin ? 1 : 0, isWin ? 0 : 1);
        }

        List<HeroResultData> resultHeroes = new List<HeroResultData>();
        var allHeroData = TableManager.Instance.HeroTable.GetAll();

        for (int i = 0; i < 32; i++)
        {
            if ((_localPlayerMap.UsedHeroBitmask & (1 << i)) != 0)
            {
                if (i < allHeroData.Count)
                {
                    var table = allHeroData[i];
                    var model = _userDataManager.HeroesModel.Find(h => h.heroId == table.id);

                    if (model != null)
                    {
                        var modelMaxExp = TableManager.Instance.HeroLevelTable.Get(model.level.ToString()).expRequirement;

                        resultHeroes.Add(new HeroResultData
                        {
                            Name       = table.heroName,
                            IconPath   = table.heroIcon,
                            Level      = model.level,
                            CurrentExp = model.exp,
                            AddedExp   = reward.HeroExp,
                            MaxExp     = modelMaxExp
                        });

                        // DB 업데이트는 여기서 그대로 진행
                        _ = _userDataManager.UpdateHero(table.id, model.level, model.exp + reward.HeroExp, model.isUnlock);
                    }
                }
            }
        }

        // 획득한 골드량과 함께 UI 호출
        //_stageUI.ShowResultPanel(isWin || isDraw, resultHeroes, reward.GoldAmount, reward.UserExp); //이건 나중에 유저 경험치도 하게된다면.
        _stageUI.ShowResultPanel(isWin || isDraw, resultHeroes, reward.GoldAmount);

        // UI 출력
        GameManager.Instance.ChangeState(GameState.Result);
    }

    public async void ShutDownAndSceneChange()
    {
        await Runner.Shutdown();

        SceneManager.LoadScene("Lobby");
    }
}
