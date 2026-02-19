public class HeroStatus
{
    public float Hp;
    public float Shield;
    public float Attack_Power;
    public float Healing_Power;
    public float Min_Attack_Speed;
    public float Max_Attack_Speed;
    public float Min_Move_Speed;
    public float Max_Move_Speed;
    public float Min_Spawn_Cooldown;
    public float Max_Spawn_Cooldown;

    public HeroStatus(HeroStatus origin)
    {
        this.Hp = origin.Hp;
        this.Shield = origin.Shield;
        this.Attack_Power = origin.Attack_Power;
        this.Healing_Power = origin.Healing_Power;
        this.Min_Attack_Speed = origin.Min_Attack_Speed;
        this.Max_Attack_Speed = origin.Max_Attack_Speed;
        this.Min_Move_Speed = origin.Min_Move_Speed;
        this.Max_Move_Speed = origin.Max_Move_Speed;
        this.Min_Spawn_Cooldown = origin.Min_Spawn_Cooldown;
        this.Max_Spawn_Cooldown = origin.Max_Spawn_Cooldown;
    }
}
