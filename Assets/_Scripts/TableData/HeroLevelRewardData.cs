using System;

[Serializable]
public class HeroLevelRewardData : ITableData
{
    public String id;
    public String heroId;
    public int level;
    public String rewardId;
    public String note;

    public string PrimaryID => id.ToString();
}
