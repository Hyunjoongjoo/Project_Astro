using System;

[Serializable]
public class HeroData : ITableData
{
    public string id;
    public string heroName;
    public string heroDesc;
    public HeroType heroType;
    public HeroRole heroRole;
    public string autoAttack;
    public string skill;
    public string heroStatId;
    public bool isDefault;
    public int goldRequirement;
    public string heroIcon;
    public string heroImg;
    public string heroModeling;
    public string heroPreviewVideo;
    public string note;

    public string PrimaryID => id.ToString();
}
