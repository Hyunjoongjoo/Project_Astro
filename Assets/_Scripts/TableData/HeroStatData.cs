using System;

[Serializable]
public class HeroStatData : ITableData
{
    public string id;
    public float spawnCooldown;
    public int BaseHp;
    public float ipLvHp;
    public int baseShield;
    public float ipLvShield;
    public int baseAttackPower;
    public float ipLvAttackPower;
    public int baseHealingPower;
    public float ipLvHealingPower;
    public float attackSpeed;
    public ArmorType armorType;
    public MoveType moveType;
    public float moveSpeed;
    public float detectionRange;
    public string note;

    public string PrimaryID => id.ToString();

    public HeroStatData() { }

    public HeroStatData(HeroStatData origin)
    {
        this.id = origin.id;
        this.spawnCooldown = origin.spawnCooldown;
        this.BaseHp = origin.BaseHp;
        this.ipLvHp = origin.ipLvHp;
        this.baseShield = origin.baseShield;
        this.ipLvShield = origin.ipLvShield;
        this.baseAttackPower = origin.baseAttackPower;
        this.ipLvAttackPower = origin.ipLvAttackPower;
        this.baseHealingPower = origin.baseHealingPower;
        this.ipLvHealingPower = origin.ipLvHealingPower;
        this.attackSpeed = origin.attackSpeed;
        this.armorType = origin.armorType;
        this.moveType = origin.moveType;
        this.moveSpeed = origin.moveSpeed;
        this.detectionRange = origin.detectionRange;
        this.note = origin.note;
    }
}
