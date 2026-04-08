using System;

[Serializable]
public class HeroData : ITableData
{
    public string id;
    public string heroName;
    public string heroDesc;
    public HeroType heroType;
    public HeroRole heroRole;
    public string heroStatId;
    public int goldRequirement;
    public string heroIcon;
    public string heroModeling;
    public string pilot_portrait;
    public string pilot_voice;
    public string note;

    public string PrimaryID => id.ToString();
}
