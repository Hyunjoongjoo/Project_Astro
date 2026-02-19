using System;

[Serializable]
public class HeroData : ITableData
{
    public String id;
    public String heroName;
    public String heroDesc;
    public HeroType heroType;
    public HeroRole heroRole;
    public String autoAttack;
    public String skill;
    public String heroStatId;
    public bool isDefault;
    public int goldRequirement;
    public String heroIcon;
    public String heroImg;
    public String heroModeling;
    public String heroPreviewVideo;
    public String note;

    public string PrimaryID => id.ToString();
}
