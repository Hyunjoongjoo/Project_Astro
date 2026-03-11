using System;

[Serializable]
public class MatchRewardData : ITableData
{
    public string id;
    public MatchType matchType;
    public MatchResult matchResult;
    public int exp;
    public int gold;
    public string note;

    public string PrimaryID => id.ToString();
}
