using System;

[Serializable]
public class UnitData : ITableData
{
    public string id;
    public string unitName;
    public string unitDesc;
    public int unitType;
    public int baseHp;
    public int baseAttackPower;
    public int baseHealingPower;
    public float attackSpeed;
    public float damageReduce;
    public int moveType;
    public float moveSpeed;
    public float detectionRange;
    public string modeling;

    public string PrimaryID => id.ToString();
}
