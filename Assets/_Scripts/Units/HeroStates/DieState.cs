using UnityEngine;

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
        {
            _unit.HeroAnimator.SetBool("IsDied", true);
            _unit.GetComponent<Collider>().enabled = false;
        }
        _unit.OnDie();
    }

    public void Exit()
    {
        _unit.UnitDespawn();
    }

    public void Update()
    {
        if (_unit.UnitType == UnitType.Hero)
        {
            AnimatorStateInfo stateInfo = _unit.HeroAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 0.95f)
                Exit();
            else
                Debug.Log("사망 애니메이션 재생 중이어야 함");
        }
        else
        {
            Exit();
        }
    }
}
