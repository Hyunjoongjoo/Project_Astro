using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum StageState
{
    WaitingForPlayers,    // 플레이어 대기 중
    AssigningTeams,       // 팀 배정 중
    ShowingPlayerInfo,    // 플레이어 정보 표시
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

    [SerializeField] private NetworkPrefabRef _minionSpawnerPrefab;

    private StageIntroUI _introUI;
    private Camera _mainCamera;

    private MatchType _curMatchType;
    private int _requiredPlayerCount;

    private const float PLAYER_INFO_DURATION = 4f;
    private const float COUNTDOWN_INTERVAL = 1f;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _introUI = FindFirstObjectByType<StageIntroUI>();
        if (_introUI == null)
            Debug.Log("인트로 UI 찾지 못함");
    }

    public void Initialize(MatchType matchType, int requiredPlayerCount)
    {
        _curMatchType = matchType;
        _requiredPlayerCount = requiredPlayerCount;
    }

    public override void Spawned()
    {
        GameManager.Instance.ChangeState(GameState.Ready);

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
        }
    }

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
            _mainCamera.transform.Rotate(new Vector3(0, 0, 180f));
 
        // DB로 부터 받아온 플레이어 정보 중 표시할 것 선정
        ShowPlayerInfo();
    }

    private void ShowPlayerInfo()
    {
        Debug.Log("플레이어 정보 표시");
        _introUI.ShowPlayerInfo();
    }

    private void UpdatePlayerInfoTimer()
    {
        StateTimer -= Runner.DeltaTime;

        if (StateTimer <= 0)
        {
            // 플레이어 정보 숨기고 카운트다운 시작
            RPC_HidePlayerInfo();
            CurrentState = StageState.Countdown;
            CountdownValue = 3;
            StateTimer = COUNTDOWN_INTERVAL;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HidePlayerInfo()
    {
        _introUI.HidePlayerInfo();
        _introUI.ShowCountdown(3);
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
                // 게임 시작!
                StartGame();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateCountdown(int value)
    {
        _introUI.UpdateCountdown(value);
    }

    private void StartGame()
    {
        CurrentState = StageState.Playing;
        ObjectContainer OC = ObjectContainer.Instance;
        OC.blueSideStructure[OC.BridgeIndex].OnDeath += BridgeDestroyed;
        OC.redSideStructure[OC.BridgeIndex].OnDeath += BridgeDestroyed;
        Debug.Log("브릿지 파괴에 메서드 구독 완료");

        RPC_StartGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartGame()
    {
        if (Object.HasStateAuthority)
            Runner.Spawn(_minionSpawnerPrefab);

        _introUI.HideCountdown();
        GameManager.Instance.ChangeState(GameState.Play);
        Debug.Log("게임 시작!");
    }

    private void BridgeDestroyed(UnitBase unit)
    {
        Debug.Log("브릿지 파괴 이벤트 메서드에 진입 완료");
        CurrentState = StageState.GameOver;

        ObjectContainer OC = ObjectContainer.Instance;
        OC.blueSideStructure[OC.BridgeIndex].OnDeath -= BridgeDestroyed;
        OC.redSideStructure[OC.BridgeIndex].OnDeath -= BridgeDestroyed;
        Debug.Log("브릿지 파괴 메서드 구독 제거 완료");

        // 브릿지가 파괴된 팀의 반대 팀이 승리 팀
        Team victory = unit.team == Team.Blue ? Team.Red : Team.Blue;

        RPC_GameOver(victory);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameOver(Team victory)
    {
        Debug.Log("게임오버 RPC 진입 성공");
        _introUI.gameObject.SetActive(true);

        _introUI.goLobbyBtn.onClick.AddListener(ShutDownAndSceneChange);

        Debug.Log($"승리팀 : {victory}, 내 팀 : {PlayerTeams.Get(Runner.LocalPlayer)}");

        // 일단 패널 아무거나 띄움. 나중에 UI매니저에게
        if (PlayerTeams.Get(Runner.LocalPlayer) == victory)
        {
            Debug.Log("승리했습니다!!");
            _introUI.ShowResultPanel(true);
        }
        else
        {
            Debug.Log("패배했습니다!!");
            _introUI.ShowResultPanel(false);
        }
        
        GameManager.Instance.ChangeState(GameState.Result);
    }

    public async void ShutDownAndSceneChange()
    {
        await Runner.Shutdown();

        SceneManager.LoadScene("Lobby");
    }
}
