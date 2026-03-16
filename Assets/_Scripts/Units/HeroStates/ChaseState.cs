using UnityEngine;

public class ChaseState : IState
{
    private UnitController _unit;

    public ChaseState(UnitController unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        if (_unit.UnitType == UnitType.Hero)
            _unit.BoosterRender = true;
    }

    public void Update()
    {
        // 타겟이 죽었거나 사라졌으면 다시 탐지 상태로 복귀
        if (_unit.currentTarget == null || _unit.currentTarget.IsDead)
        {
            _unit.currentTarget = null;
            _unit.StateMachine.ChangeState(_unit.DetectState);
            return;
        }

        // 타겟과의 거리 계산
        float distance = Vector3.Distance(_unit.transform.position, _unit.currentTarget.transform.position);

        // 공격 사거리 이내라면 공격 상태로 전환
        if (distance <= _unit.attackRange)
            _unit.StateMachine.ChangeState(_unit.AttackState);
        else
            // 사거리 밖이라면 계속 타겟을 향해 이동
            _unit.MoveTo(_unit.currentTarget.transform.position);
    }

    public void Exit()
    {
        // 추적을 끝내고 다른 상태로 넘어갈 때 멈춤
        _unit.StopMove();
        if (_unit.UnitType == UnitType.Hero)
            _unit.BoosterRender = false;
    }
}
