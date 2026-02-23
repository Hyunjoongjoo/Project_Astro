//데이터클래스 확장메서드 모음

public static class TableExtensions
{
    public static HeroStatData Clone(this HeroStatData origin)
    {
        return new HeroStatData()
        {
            id = origin.id,
            spawnCooldown = origin.spawnCooldown,
            BaseHp = origin.BaseHp,
            ipLvHp = origin.ipLvHp,
            baseShield = origin.baseShield,
            ipLvShield = origin.ipLvShield,
            baseAttackPower = origin.baseAttackPower,
            ipLvAttackPower = origin.ipLvAttackPower,
            baseHealingPower = origin.baseHealingPower,
            ipLvHealingPower = origin.ipLvHealingPower,
            attackSpeed = origin.attackSpeed,
            armorType = origin.armorType,
            moveType = origin.moveType,
            moveSpeed = origin.moveSpeed,
            detectionRange = origin.detectionRange,
            note = origin.note
        };
    }
    



    
}
