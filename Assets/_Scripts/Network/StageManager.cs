using Fusion;
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

public class StageManager : NetworkBehaviour
{
    [Networked, HideInInspector] public StageState CurrentState { get; set; }
    [Networked, HideInInspector] public float StateTimer { get; set; }
    [Networked, HideInInspector] public int CountdownValue { get; set; }

    // 플레이어별 팀 정보 (PlayerRef를 키로 사용)
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, Team> PlayerTeams => default;

    //플레이어별 증강 선택 완료 여부 추적용
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, NetworkBool> _playerAugmentReady => default;

    [Networked, Capacity(2)]
    public NetworkDictionary<Team, int> AugmentExp => default;

    [SerializeField] private NetworkPrefabRef _minionSpawnerPrefab;

    ObjectContainer _objectContainer;
    private StageUI _stageUI;
    private Camera _mainCamera;

    private MatchType _curMatchType;
    private int _requiredPlayerCount;

    private readonly int GAME_DURATION = 240;
    private readonly float PLAYER_INFO_DURATION = 4f;
    private readonly float COUNTDOWN_INTERVAL = 1f;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _stageUI = FindFirstObjectByType<StageUI>();
        if (_stageUI == null)
            Debug.Log("스테이지 UI 찾지 못함");
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
        if (Runner.ActivePlayers.Count() == _requiredPlayerCount)
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

        // 앞 절반 블루팀, 뒤 절반 레드팀
        for (int i = 0; i < half; i++)
            PlayerTeams.Add(players[i], Team.Blue);

        for (int i = half; i < players.Count; i++)
            PlayerTeams.Add(players[i], Team.Red);

        // 팀 자원인 증강 게이지도 추가함.
        AugmentExp.Add(Team.Blue, 0);
        AugmentExp.Add(Team.Red, 0);

        // RPC로 모든 클라이언트에 팀 배정 알림
        RPC_NotifyTeamAssignment();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyTeamAssignment()
    {
        Team myTeam = PlayerTeams.Get(Runner.LocalPlayer);

        Debug.Log($"팀 배정 완료! 내 팀 {myTeam}");

        GameManager.Instance.SetTeam(myTeam);

        if (myTeam == Team.Red)
        {
            _mainCamera.transform.Rotate(new Vector3(0, 0, 180f));
            _stageUI.InitRedTeam();
        }
 
        // DB로 부터 받아온 플레이어 정보 중 표시할 것 선정
        ShowPlayerInfo();
    }

    private void ShowPlayerInfo()
    {
        Debug.Log("플레이어 정보 표시");
        _stageUI.ShowPlayerInfo();
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
        var options = AugmentManager.Instance.GetRandomAugments(AugmentType.Hero, 3);
        AugmentManager.Instance.ShowAugmentWindow(options);

        Debug.Log("[Stage] 증강 선택 시작!");
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

    private void DecreaseAugmentGauge(Team team, int amount)
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
        Team myTeam = PlayerTeams.Get(Runner.LocalPlayer);
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

        Debug.Log($"승리팀 : {victory}, 내 팀 : {PlayerTeams.Get(Runner.LocalPlayer)}");

        Team myTeam = PlayerTeams.Get(Runner.LocalPlayer);

        // 일단 패널 아무거나 띄움. 나중에 UI매니저에게
        if (victory == Team.None) // 무승부도 존재하는 것으로 보임. 승리팀이 없으면 무승부
        {
            Debug.Log("무승부입니다.");
            // TODO : bool 값으로 승리 또는 패배만 띄우는데 무승부 처리도 필요
            _stageUI.ShowResultPanel(true); 
        }
        else if (myTeam == victory)
        {
            Debug.Log("승리했습니다!!");
            _stageUI.ShowResultPanel(true);
        }
        else
        {
            Debug.Log("패배했습니다!!");
            _stageUI.ShowResultPanel(false);
        }
        
        GameManager.Instance.ChangeState(GameState.Result);
    }

    public async void ShutDownAndSceneChange()
    {
        await Runner.Shutdown();

        SceneManager.LoadScene("Lobby");
    }
}
