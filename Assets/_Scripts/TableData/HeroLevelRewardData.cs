using System;

[Serializable]
public class HeroLevelRewardData : ITableData
{
    public string id;
    public string heroId;
    public int level;
    public string rewardId;
    public string note;

    public string PrimaryID => id.ToString();
}
