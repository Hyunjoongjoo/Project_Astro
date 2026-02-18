using Fusion;
using System.Linq;
using UnityEngine;

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
    [Networked, Capacity(2)]
    public NetworkDictionary<PlayerRef, Team> PlayerTeams => default;

    [SerializeField] private NetworkPrefabRef _minionSpawnerPrefab;

    private StageIntroUI _introUI;
    private Camera _mainCamera;

    private const float PLAYER_INFO_DURATION = 4f;
    private const float COUNTDOWN_INTERVAL = 1f;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _introUI = FindFirstObjectByType<StageIntroUI>();
        if (_introUI == null)
            Debug.Log("인트로 UI 찾지 못함");
    }

    public override void Spawned()
    {
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
        if (Runner.ActivePlayers.Count() == 2)
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

        // 앞의 사람 블루팀, 뒤의 사람 레드팀 (일단 1:1 기준)
        PlayerTeams.Add(players[0], Team.Blue);
        PlayerTeams.Add(players[1], Team.Red);

        // RPC로 모든 클라이언트에 팀 배정 알림
        RPC_NotifyTeamAssignment();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyTeamAssignment()
    {
        Debug.Log("팀 배정 완료!");

        Team myTeam = PlayerTeams.Get(Runner.LocalPlayer);

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
}
