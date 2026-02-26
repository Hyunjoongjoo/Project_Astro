using UnityEngine;

public enum SceneState { Title, Lobby, Stage }
public enum GameState { Ready, Play, Result } //추후 상태 추가가능

// 게임 매니저의 경우 아직 어떤 역할을 할지 구체적으로 정해지지 않음
// 게임 전체 흐름을 관리할 것 같음. 어떤식으로 관리할지 설계해야 함.

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState _currentState = GameState.Ready;
    [SerializeField] private SceneState _flowState = SceneState.Title;

    //다른데서 참조할 게임시작여부
    public bool IsGameStarted => _currentState == GameState.Play;
    public SceneState FlowState => _flowState;


    public Team PlayerTeam { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Application.targetFrameRate = 60;
    }

    public void SetTeam(Team team)
    {
        PlayerTeam = team;
        Debug.Log($"[GameManager] {PlayerTeam}으로 세팅 완료!");
    }

    public void ChangeState(GameState newState)
    {
        if(_currentState == newState) return;

        _currentState = newState;

        switch (_currentState)
        {
            case GameState.Ready:
                var options = AugmentManager.Instance.GetRandomAugments(AugmentType.Hero, 3);
                AugmentManager.Instance.ShowAugmentWindow(options);
                break;
            case GameState.Play:
                //모든 로직 가동
                break;
            case GameState.Result:
                // 결과 처리
                break;
        }
    }

    public void SetSceneState(SceneState state)
    {
        _flowState = state;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm(state);
    }

    public void OnAugmentSelectionComplete() //초기 증강 선택 완료 버튼에서 호출할 메서드
    {
        ChangeState(GameState.Play);
    }
}
