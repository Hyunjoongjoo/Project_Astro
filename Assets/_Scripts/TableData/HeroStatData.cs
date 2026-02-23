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
}
