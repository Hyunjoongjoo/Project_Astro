
public class DieState : IState
{
    private UnitController _unit;

    public DieState(UnitController unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        if (_unit.UnitType == UnitType.Hero)
            _unit.HeroAnimator.SetBool("IsDied", true);
        _unit.OnDie();
    }

    public void Exit()
    {
        _unit.UnitDespawn();
    }

    public void Update()
    {
        Exit();
    }
}
