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
        // 진입하면서 스킬을 실행한다. 이 안에서 선딜 -> 캐스팅 -> 후딜 순으로 재생한다.
        _hero.curUniqueSkill.Execute();
    }

    public void Update()
    {
        // 영웅의 스킬이 다른 액션을 차단하지 않거나, 시전 중이 아니라면 이전 상태로 돌아감
        if (_hero.curUniqueSkill.Data.blockAction == false || _hero.curUniqueSkill.IsCasting == false) 
        {
            _hero.StateMachine.ChangePreviousState();
        }           
    }

    public void Exit()
    {
        
    }
}
