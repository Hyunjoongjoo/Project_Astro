using System;

[Serializable]
public class HeroLevelData : ITableData
{
    public int level;
    public int expRequirement;
    public int goldRequirement;
    public String note;

    public string PrimaryID => level.ToString();
}
