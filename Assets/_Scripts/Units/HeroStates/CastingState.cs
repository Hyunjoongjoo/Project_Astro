using UnityEngine;

public class CastingState : IState
{
    private HeroController _hero;

    public CastingState(HeroController hero)
    {
        _hero = hero;
    }

    public void Enter()
    {
        Debug.Log("Casting 상태 진입");
        _hero.HeroAnimator.SetTrigger("SkillTrigger");
        _hero.curUniqueSkill.Execute();
    }

    public void Update()
    {
        if (_hero.curUniqueSkill.IsCasting == false)
            _hero.StateMachine.ChangePreviousState();
    }

    public void Exit()
    {
        Debug.Log("Casting 상태 탈출");
    }
}
