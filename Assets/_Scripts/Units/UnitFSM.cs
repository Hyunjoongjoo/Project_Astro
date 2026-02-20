public enum UnitAIState
{
    Deploy, Detect, Attack, Dead
}
public class UnitFSM
{
    public UnitAIState State { get; private set; } = UnitAIState.Detect;

    public void ForceDead()
    {
        State = UnitAIState.Dead;
    }

    public void Update(bool isDead, bool hasTarget, bool inRange)
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
