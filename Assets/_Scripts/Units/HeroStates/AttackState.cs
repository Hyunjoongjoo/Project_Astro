using Fusion;
using UnityEngine;

public class AttackState : IState
{
    private NewHeroController _hero;
    private TickTimer _attackTimer;

    public AttackState(NewHeroController hero)
    {
        _hero = hero;
    }

    public void Enter()
    {
        // 공격 상태 진입 시 이동 멈추고
        _hero.StopMove();

        // 진입 직후 바로 첫 공격이 가능하도록 타이머를 0초로 세팅
        _attackTimer = TickTimer.CreateFromSeconds(_hero.Runner, 0f);
    }

    public void Update()
    {
        // 타겟 유효성 검사 (타겟이 죽었거나 없으면 다시 탐지)
        if (_hero.currentTarget == null || _hero.currentTarget.IsDead)
        {
            _hero.StateMachine.ChangeState(_hero.DetectState);
            return;
        }

        // 사거리 검사 (타겟이 멀어지면 다시 추적)
        // 성능 최적화를 위해 Vector3.Distance 대신 sqrMagnitude
        Vector3 diff = _hero.currentTarget.transform.position - _hero.transform.position;
        if (diff.sqrMagnitude > _hero.attackRange * _hero.attackRange)
        {
            _hero.StateMachine.ChangeState(_hero.ChaseState);
            return;
        }

        // 타겟 바라보기
        RotateToTarget(diff);

        // 공격 실행
        if (_attackTimer.ExpiredOrNotRunning(_hero.Runner))
        {
            PerformAttack();

            // 공속에 맞춰 다음 공격 타이머 설정
            float attackCooldown = _hero.attackSpeed > 0f ? 1f / _hero.attackSpeed : 1f;
            _attackTimer = TickTimer.CreateFromSeconds(_hero.Runner, attackCooldown);
        }
    }

    public void Exit()
    {

    }

    private void RotateToTarget(Vector3 direction)
    {
        direction.y = 0f; // 수평 회전만 적용
        if (direction.sqrMagnitude > 0.001f)
        {
            _hero.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void PerformAttack()
    {
        // HeroController가 가진 IBasicAttack 인터페이스와 RPC 로직을 활용합니다.
        if (_hero.attackType == AttackType.Melee) // HeroController에 attackType 변수 노출 필요
        {
            ((IBasicAttack)_hero).BaseAttack(_hero.currentTarget);
        }
        else
        {
            _hero.AttackRanged(_hero.currentTarget.transform.position); // 이 메서드는 public이어야 함
        }
    }
}
