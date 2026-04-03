using Fusion;
using UnityEngine;

public class DeployState : IState
{
    private HeroController _hero;

    private Vector3 _deployTarget;
    private TickTimer _deployDelayTimer;
    private TickTimer _deployFailSafeTimer;

    public DeployState(HeroController hero)
    {
        _hero = hero;
    }

    public void SetDeployData(Vector3 targetPos, float deployDelay)
    {
        _deployTarget = targetPos;
        _deployDelayTimer = TickTimer.CreateFromSeconds(_hero.Runner, deployDelay);
        _deployFailSafeTimer = TickTimer.CreateFromSeconds(_hero.Runner, deployDelay + 2f);
    }

    public void Enter()
    {
        _hero.agent.speed = float.Parse(
            TableManager.Instance.ConfigTable.Get("deployment_speed").configValue
            );
        _hero.MoveTo(_deployTarget);
        _hero.HeroAnimator.SetBool("IsDeploying", true);
    }

    public void Update()
    {
        // 배치 딜레이가 아직 끝나지 않았다면 대기
        if (!_deployDelayTimer.Expired(_hero.Runner))
            return;

        // 경로를 잃었다면 다시 설정
        if (!_hero.agent.hasPath)
            _hero.MoveTo(_deployTarget);

        float dist = Vector3.Distance(_hero.transform.position, _deployTarget);
        bool reachedTarget = (!_hero.agent.pathPending && _hero.agent.remainingDistance <= _hero.agent.stoppingDistance) || dist < 0.5f;

        // 목적지에 도착했거나, 충돌 등으로 너무 오래 걸려 FailSafe 타이머가 터졌다면 배치 종료
        if (reachedTarget || _deployFailSafeTimer.Expired(_hero.Runner))
            // 배치 완료 시 바로 탐지 상태로
            _hero.StateMachine.ChangeState(_hero.DetectState);
    }

    public void Exit()
    {
        _hero.agent.speed = _hero.MoveSpeed;
        _hero.HeroAnimator.SetBool("IsDeploying", false);
        // 기존 FinishDeploy()에 있던 초기화 로직
        _hero.agent.ResetPath();
    }
}
