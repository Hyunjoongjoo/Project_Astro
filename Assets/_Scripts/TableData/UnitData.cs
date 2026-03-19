using System;

[Serializable]
public class UnitData : ITableData
{
    public string id;
    public string unitName;
    public string unitDesc;
    public UnitType unitType;
    public int baseHp;
    public int baseAttackPower;
    public int baseHealingPower;
    public float attackSpeed;
    public float damageReduce;
    public MoveType moveType;
    public float moveSpeed;
    public float detectionRange;
    public string modeling;

    public string PrimaryID => id.ToString();
}
