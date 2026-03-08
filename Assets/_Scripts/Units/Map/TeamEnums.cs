public enum Team
{
    None, Blue, Red
}

public enum UnitType
{
    Bridge, Tower, Hero, Minion
}

public enum UnitState
{ 
    Idle, Move, Attack, Dead, Skill 
}

public enum AttackType
{
    Melee, Range
}

public enum SkillType
{
    Normal,
    Standard,
    A_Type,
    A_Type_Enhanced,
    B_Type,
    B_Type_Enhanced
}

public enum EffectAttachType
{
    World,      // 그냥 월드에 생성
    Caster,     // 시전자에 붙음
    Target      // 타겟에 붙음
}