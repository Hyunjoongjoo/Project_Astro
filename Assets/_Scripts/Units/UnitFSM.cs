using Fusion;
using static Unity.Collections.Unicode;

public enum UnitAIState//상태 정의 : 배치(영웅만 사용),탐지,공격,사망
{
    Deploy, //영웅컨트롤러쪽에서 직접 처리하는 상태
    //전투 AI 상태
    Detect, Attack, Skill, Dead
}
public class UnitFSM
{
    public UnitAIState State { get; private set; } = UnitAIState.Detect;
    private TickTimer _skillTimer;

    public void ForceDead()//즉시 사망 상태로 전환
    {
        State = UnitAIState.Dead;
    }
    public void ForceDetect()//즉시 탐지 상태로 전환
    {
        State = UnitAIState.Detect;
    }

    public void EnterSkill(NetworkRunner runner, float duration)
    {
        State = UnitAIState.Skill;
        _skillTimer = TickTimer.CreateFromSeconds(runner, duration);
    }

    public void TickSkill(NetworkRunner runner)
    {
        if (State == UnitAIState.Skill)
        {
            if (_skillTimer.Expired(runner))
            {
                State = UnitAIState.Detect;
            }
        }
    }

    //상태 전이 판단, 실제 행동은 컨트롤러에서 처리
    public void DecideState(bool isDead, bool hasTarget, bool inRange)
    {
        if (isDead)
        {
            State = UnitAIState.Dead;
            return;
        }

        if (State == UnitAIState.Skill)
        {
            return;
        }


        switch (State)
        {
            case UnitAIState.Detect:
                if (hasTarget && inRange)
                {
                    State = UnitAIState.Attack;
                }
                break;

            case UnitAIState.Attack:
                if (!hasTarget || !inRange)
                {
                    State = UnitAIState.Detect;
                }
                break;
        }
    }
}
