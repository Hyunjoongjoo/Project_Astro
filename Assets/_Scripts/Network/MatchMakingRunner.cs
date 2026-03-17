using Fusion;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MatchMakingRunner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    private Button _cancelBtn;
    private NetworkRunner _networkRunner;

    private MatchType _curMatchType;
    private int _requiredPlayerCount;

    // 매치 대기 타이머 코루틴을 추적하기 위한 변수
    private Coroutine _matchTimerCoroutine;
    private readonly float WAIT_TIME_FOR_DUMMY = 15f; // 15초 대기

    private bool _existDummy = false;

    public void Initialize(Button btn, NetworkRunner runner)
    {
        _cancelBtn = btn;
        _networkRunner = runner;
    }

    // 포톤 서버에 접속 시도.
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer 실행됨.");
        if (_networkRunner.SessionInfo.Properties.TryGetValue(MatchMakingSystem.MATCH_TYPE, out var value))
        {
            _curMatchType = (MatchType)(int)value;
            _requiredPlayerCount = _curMatchType == MatchType.OneVsOne ? 2 : 4;
        }

        _cancelBtn.interactable = true;
    }

    // 매치메이킹 세션에 플레이어가 들어옴.
    // 아직 MMR 기준 매칭 분리라던가 기준은 없음.
    public void PlayerJoined(PlayerRef player) 
    {
        Debug.Log($"플레이어 입장: {player.PlayerId}");

        if (_networkRunner.IsSharedModeMasterClient)
        {
            CheckMatchStatus(_requiredPlayerCount);

            // 인원이 아직 다 안 찼고, 타이머가 돌고 있지 않다면 타이머 시작
            if (_networkRunner.ActivePlayers.Count() < _requiredPlayerCount && _matchTimerCoroutine == null)
            {
                _matchTimerCoroutine = StartCoroutine(DummyMatchTimerCoroutine());
            }
        }
    }

    // 15초 대기 타이머 코루틴
    private IEnumerator DummyMatchTimerCoroutine()
    {
        Debug.Log($"[매치메이킹] {_requiredPlayerCount}명을 기다립니다. {WAIT_TIME_FOR_DUMMY}초 후 더미 매칭으로 전환됩니다.");

        yield return new WaitForSeconds(WAIT_TIME_FOR_DUMMY);

        Debug.Log("[매치메이킹] 대기 시간 초과! 빈자리를 더미 클라이언트로 간주하고 게임을 시작합니다.");

        // 시간이 다 되면 인원수와 상관없이 매칭 강제 완료 처리
        _existDummy = true;
        StartIngame();
    }

    public void CheckMatchStatus(int count)
    {
        if (_networkRunner.ActivePlayers.Count() == count)
        {
            Debug.Log($"플레이어 {count}명 입장! 매칭 완료");

            // 정상적으로 인원이 다 차면 진행 중이던 대기 타이머를 취소.
            if (_matchTimerCoroutine != null)
            {
                StopCoroutine(_matchTimerCoroutine);
                _matchTimerCoroutine = null;
            }
            _existDummy = false;
            StartIngame();
        }
    }

    // 매칭 완료 후 씬을 로드하는 공통 로직
    private void StartIngame()
    {
        _networkRunner.SessionInfo.IsOpen = false;

        int index = UnityEngine.SceneManagement.SceneUtility.
            GetBuildIndexByScenePath("Assets/_Scenes/Stage.unity");

        _networkRunner.LoadScene(SceneRef.FromIndex(index));
    }

    public void PlayerLeft(PlayerRef player)
    {
        Debug.Log("플레이어가 떠남");
        // 만약 스테이지 씬 상태에서 누군가 떠났다면
        if (GameManager.Instance != null && 
            GameManager.Instance.FlowState == SceneState.Stage)
        {

            if (StageManager.Instance != null)
            {
                StartCoroutine(MomentWaitingAndExitGame());
            }
            else
            {
                Debug.Log("StageManager 체크에서 false");
            }
        }
        else
        {
            Debug.Log("GameManager 체크에서 false");
        }
    }

    // 플레이어 누군가가 탈주하여 게임을 더 진행할 수 없음
    // 안내 팝업을 3초정도 띄우고 로비로 강제 이동
    private IEnumerator MomentWaitingAndExitGame()
    {
        StageManager.Instance.NetworkExceptionUiControl(true, false);
        yield return new WaitForSeconds(3f);
        StageManager.Instance.ShutDownAndSceneChange();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("씬 로드 스타트 콜백 실행됨");
    }

    public void OnSceneLoaded(NetworkRunner runner)
    {
        Debug.Log("씬 로드 완료 콜백 실행됨");
        GameManager.Instance.SetSceneState(SceneState.Stage);

        if (runner.IsSharedModeMasterClient)
        {
            var stageManagerPrefab = Resources.Load<NetworkObject>("StageManager");
            runner.Spawn(stageManagerPrefab, Vector3.zero, Quaternion.identity,
                onBeforeSpawned: (Runner, obj) => 
                {
                    StageManager stageManager = obj.GetComponent<StageManager>();
                    stageManager.Initialize(_curMatchType, _requiredPlayerCount, _existDummy);
                });
            
            Debug.Log("마스터 클라이언트 StageManager 생성 완료");
        }
    }
}
