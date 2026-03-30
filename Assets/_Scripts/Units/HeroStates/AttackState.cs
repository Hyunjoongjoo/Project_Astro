using Fusion;
using System;
using UnityEngine;

public class AttackState : IState
{
    private UnitController _unit;
    private TickTimer _attackTimer;

    // 평타를 시전하는 것이 스킬의 조건 등 다른 곳에 영향을 끼치는 경우 구독.
    public Action OnNormalAttack;

    public AttackState(UnitController unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        // 공격 상태 진입 시 이동 멈추고
        _unit.StopMove();

        // 진입 직후 바로 첫 공격이 가능하도록 타이머를 0초로 세팅
        //if (_attackTimer.ExpiredOrNotRunning(_unit.Runner))
        //    _attackTimer = TickTimer.CreateFromSeconds(_unit.Runner, 0f);
    }

    public void Update()
    {
        // 타겟 유효성 검사 (타겟이 죽었거나 없으면 다시 탐지)
        if (_unit.currentTarget == null || _unit.currentTarget.IsDead)
        {
            _unit.StateMachine.ChangeState(_unit.DetectState);
            return;
        }

        // 사거리 검사 (타겟이 멀어지면 다시 추적)
        // 성능 최적화를 위해 Vector3.Distance 대신 sqrMagnitude
        Vector3 diff = _unit.currentTarget.transform.position - _unit.transform.position;
        if (diff.sqrMagnitude > _unit.attackRange * _unit.attackRange)
        {
            _unit.StateMachine.ChangeState(_unit.ChaseState);
            return;
        }

        // 타겟 바라보기
        _unit.RotateToTarget(diff);

        // 공격 실행
        //if (_attackTimer.ExpiredOrNotRunning(_unit.Runner))
        //{
        //    PerformAttack();

        //    // 공속에 맞춰 다음 공격 타이머 설정
        //    float attackCooldown = _unit.AttackSpeed > 0f ? 1f / _unit.AttackSpeed : 1f;
        //    _attackTimer = TickTimer.CreateFromSeconds(_unit.Runner, attackCooldown);
        //}

        //Skill이 쿨을 관리하므로 여기서는 조건만 체크
        if (_unit.normalAttack != null &&
            _attackTimer.ExpiredOrNotRunning(_unit.Runner) &&
            _unit.normalAttack.UsingConditionCheck())
        {
            PerformAttack();
            _attackTimer = TickTimer.CreateFromSeconds(_unit.Runner, _unit.UnitStat.AttackSpeed.Value);
        }
    }

    public void Exit()
    {
        
    }

    private void PerformAttack()
    {
        _unit.normalAttack.Execute();
        OnNormalAttack?.Invoke();
    }
}
