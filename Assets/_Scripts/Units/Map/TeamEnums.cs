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
    normal_attack,
    normal_skill,
    augment_skill,
    augment_skill_enhance
}

public enum EffectAttachType
{
    World,      // 그냥 월드에 생성
    Caster,     // 시전자에 붙음
    Target      // 타겟에 붙음
}