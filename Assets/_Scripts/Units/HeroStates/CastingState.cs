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
        _hero.curUniqueSkill.Execute();
    }

    public void Update()
    {
        if (_hero.curUniqueSkill.IsCasting == false)
            _hero.StateMachine.ChangePreviousState();
    }

    public void Exit()
    {
        
    }
}
