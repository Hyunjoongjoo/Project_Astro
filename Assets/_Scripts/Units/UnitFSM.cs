public enum UnitAIState//상태 정의 : 배치(영웅만 사용),탐지,공격,사망
{
    Deploy, Detect, Attack, Dead
}
public class UnitFSM
{
    public UnitAIState State { get; private set; } = UnitAIState.Detect;

    public void ForceDead()//즉시 사망 상태로 전환
    {
        State = UnitAIState.Dead;
    }

    //상태 전이 판단, 실제 행동은 컨트롤러에서 처리
    public void FSMUpdate(bool isDead, bool hasTarget, bool inRange)
    {
        if (isDead)
        {
            State = UnitAIState.Dead;
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
