using Fusion;
using UnityEngine;

public class DetectState : IState
{
    private NewHeroController _hero;
    private TickTimer _searchTimer;

    public DetectState(NewHeroController hero)
    {
        _hero = hero;
    }

    public void Enter()
    {
        Debug.Log("Detect 상태 진입");
        _hero.StopMove();
        // 진입 시 바로 탐색할 수 있도록 타이머 초기화
        _searchTimer = TickTimer.CreateFromSeconds(_hero.Runner, 0f);
    }

    public void Update()
    {
        // 일정 주기마다 타겟 탐색
        if (_searchTimer.ExpiredOrNotRunning(_hero.Runner))
        {
            _hero.currentTarget = _hero.FindTarget();

            if (_hero.currentTarget != null)
            {
                // 타겟을 찾았다면 추적 상태로 전환
                _hero.StateMachine.ChangeState(_hero.ChaseState);
                return;
            }

            // 타겟이 없다면 가까운 포탑으로 이동하며 타이머 다시 세팅
            _hero.MoveTo(_hero.GetClosestTower().transform.position);
            _searchTimer = TickTimer.CreateFromSeconds(_hero.Runner, _hero.searchInterval);
        }
    }

    public void Exit()
    {
        _hero.StopMove();
    }
}
