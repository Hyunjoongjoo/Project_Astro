using Fusion;
using UnityEngine;

public class DetectState : IState
{
    private UnitController _unit;
    private TickTimer _searchTimer;

    public DetectState(UnitController unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        _unit.StopMove();
        // 진입 시 바로 탐색할 수 있도록 타이머 초기화
        _searchTimer = TickTimer.CreateFromSeconds(_unit.Runner, 0f);
    }

    public void Update()
    {
        // 일정 주기마다 타겟 탐색
        if (_searchTimer.ExpiredOrNotRunning(_unit.Runner))
        {
            _unit.currentTarget = _unit.FindTarget();

            if (_unit.currentTarget != null)
            {
                // 타겟을 찾았다면 추적 상태로 전환
                _unit.StateMachine.ChangeState(_unit.ChaseState);
                return;
            }

            // 타겟이 없다면 가까운 포탑으로 이동하며 타이머 다시 세팅
            _unit.MoveTo(_unit.GetClosestTower().transform.position);
            _searchTimer = TickTimer.CreateFromSeconds(_unit.Runner, _unit.searchInterval);
        }
    }

    public void Exit()
    {
        _unit.StopMove();
    }
}
