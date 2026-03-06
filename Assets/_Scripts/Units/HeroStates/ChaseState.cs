using UnityEngine;

public class ChaseState : IState
{
    private NewHeroController _hero;

    public ChaseState(NewHeroController hero)
    {
        _hero = hero;
    }

    public void Enter()
    {

    }

    public void Update()
    {
        // 타겟이 죽었거나 사라졌으면 다시 탐지 상태로 복귀
        if (_hero.currentTarget == null || _hero.currentTarget.IsDead)
        {
            _hero.currentTarget = null;
            _hero.StateMachine.ChangeState(_hero.DetectState);
            return;
        }

        // 타겟과의 거리 계산
        float distance = Vector3.Distance(_hero.transform.position, _hero.currentTarget.transform.position);

        // 공격 사거리 이내라면 공격 상태로 전환
        if (distance <= _hero.attackRange)
            _hero.StateMachine.ChangeState(_hero.AttackState);
        else
            // 사거리 밖이라면 계속 타겟을 향해 이동
            _hero.MoveTo(_hero.currentTarget.transform.position);
    }

    public void Exit()
    {
        // 추적을 끝내고 다른 상태로 넘어갈 때 멈춤
        _hero.StopMove();
    }
}
