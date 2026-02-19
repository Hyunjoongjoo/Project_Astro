using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MatchMakingRunner : SimulationBehaviour, IPlayerJoined
{
    private Button _cancelBtn;
    private NetworkRunner _networkRunner;

    public void Initialize(Button btn, NetworkRunner runner)
    {
        _cancelBtn = btn;
        _networkRunner = runner;
    }

    // 포톤 서버에 접속 시도.
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer 실행됨.");
        _cancelBtn.interactable = true;
    }

    // 매치메이킹 세션에 플레이어가 들어옴.
    // 아직 MMR 기준 매칭 분리라던가 기준은 없음.
    public void PlayerJoined(PlayerRef player) 
    {
        Debug.Log($"플레이어 입장: {player.PlayerId}");

        if (_networkRunner.IsSharedModeMasterClient && _networkRunner.ActivePlayers.Count() == 2)
        {
            Debug.Log("플레이어 2명 입장! 매칭 완료");

            int index = UnityEngine.SceneManagement.SceneUtility.
                GetBuildIndexByScenePath("Assets/_Scenes/Stage.unity");

            _networkRunner.LoadScene(SceneRef.FromIndex(index));
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("씬 로드 스타트 콜백 실행됨");
    }

    public void OnSceneLoaded(NetworkRunner runner)
    {
        Debug.Log("씬 로드 완료 콜백 실행됨");

        if (runner.IsSharedModeMasterClient)
        {
            var stageManagerPrefab = Resources.Load<NetworkObject>("StageManager");
            runner.Spawn(stageManagerPrefab, Vector3.zero, Quaternion.identity);

            Debug.Log("마스터 클라이언트 StageManager 생성 완료");
        }
    }
}
