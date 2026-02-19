using UnityEngine;

public enum GameState { Ready, Play, Result } //추후 상태 추가가능

// 게임 매니저의 경우 아직 어떤 역할을 할지 구체적으로 정해지지 않음
// 게임 전체 흐름을 관리할 것 같음. 어떤식으로 관리할지 설계해야 함.

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState _currentState = GameState.Ready;

    //다른데서 참조할 게임시작여부
    public bool IsGameStarted => _currentState == GameState.Play;

    public Team PlayerTeam { get; private set; }

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
                //증강 선택 UI같은거 띄우기
                break;
            case GameState.Play:
                //모든 로직 가동
                break;
            case GameState.Result:
                // 결과 처리
                break;
        }
    }

    public void OnAugmentSelectionComplete() //초기 증강 선택 완료 버튼에서 호출할 메서드
    {
        ChangeState(GameState.Play);
    }
}
