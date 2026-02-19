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
    DecreaseCooldown,      

    IncreaseMoveSpeed,
    DecreaseMoveSpeed,

    DecreaseDamageTaken,
    IncreaseDamageTaken,

    IncreaseMaxHp,
    DecreaseMaxHp,

    InstantHeal,
    IncreaseHealPower,
    IncreaseShieldAmount,

    DecreaseRespawnTime,
    ImmuneCcCount
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
public enum TargetType
{
    None,
    Self,
    Team, 
    NearestEnemy
}