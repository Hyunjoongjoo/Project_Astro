// 평타 공격 기능은 인터페이스로 정의하여 붙여줌.
public interface IBasicAttack
{
    public float AttackPower { get; }
    public float AttackSpeed { get; }
    public float AttackRange { get; }

    public void BaseAttack(UnitBase target)
    {
        target.TakeDamage(AttackPower);
    }
}
