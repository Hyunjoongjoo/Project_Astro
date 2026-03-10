//Table에 존재하는 Enum 정의
//CSV, 엑셀 데이터랑 1:1 매핑용

public enum ItemType
{
    None,
    Attack,     
    Defense,    
    Utility,  
    Hybrid     
}

public enum HeroType
{
    None,
    Robot,
    SpaceCraft
}

public enum HeroRole
{
    None,
    Tank,       
    Melee,
    Ranged,    
    Summoner,
    Healer      
}

//효과타입, CSV 언더바 제거버전
public enum EffectType
{
    None,
    IncreaseAttackPower,   
    IncreaseAttackSpeed, 
    IncreaseAttackRange,
    IncreaseSkillRange,
    IncreaseDetectionRange, //탐지 범위
    DecreaseCooldown,
    DecreaseAttackSpeed,

    IncreaseMoveSpeed,
    DecreaseMoveSpeed,

    DecreaseDamageTaken,
    IncreaseDamageTaken,

    IncreaseMaxHp,
    DecreaseMaxHp,

    InstantHeal,            //유닛스탯제외(즉시회복)
    IncreaseHealPower,
    IncreaseShieldAmount,   //유닛스탯제외2(실드증가) => 기본스탯에 실드없어서 매핑 제외

    DecreaseRespawnTime,
    ImmuneCcCount           //유닛스탯제외3(CC면역횟수)
}



public enum UnitType
{
    Bridge, Tower, Hero, Minion
}

public enum SkillType
{
    normal_attack,
    normal_skill,
    augment_skill,
    augment_skill_enhance,
    passive_skill
}

public enum ArmorType 
{
    Light_Armor, Medium_Armor, Heavy_Armor 
}
public enum MoveType 
{
    Small, Large, Fixed 
}

//조건 타입 정의
public enum TriggerCondition
{
    None,
    Passive,            
    HpBelow,           
    HpAbove,            
    OnHit,             
    OnSpawn,         
    InternalCooldown
}

//효과 적용 대상
public enum Target
{
    None,
    Self,
    Team, 
    NearestEnemy
}

public enum StatType
{
    Hp,                 // 체력
    AttackPower,        // 공격력
    HealingPower,       // 치유력
    AttackSpeed,        // 공격 속도
    MoveSpeed,          // 이동 속도
    DamageReduction,    // 받는 피해량 감소
    SkillCooldown,      // 스킬 쿨타임 감소
    RespawnTime,        // 재소환 대기시간
    DetectionRange,      // 탐지 범위
    MoveType
}

public enum Team
{
    None, Blue, Red
}