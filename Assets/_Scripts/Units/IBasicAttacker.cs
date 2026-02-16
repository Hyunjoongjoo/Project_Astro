// 평타 공격 기능은 인터페이스로 정의하여 붙여줌.
public interface IBasicAttack
{
    public float AttackPower { get; set; }
    public float AttackSpeed { get; set; }
    public float AttackRange { get; set; }

    public void BaseAttack(UnitBase target)
    {
        target.TakeDamage(AttackPower);
    }
}
