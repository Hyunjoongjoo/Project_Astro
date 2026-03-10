using System;

[Serializable]
public class SkillInfoData : ITableData
{
    public string id;
    public string heroId;
    public SkillType skillType;
    public string skillName;
    public string skillDes;
    public string skillIcon;
    public bool isOpened;
    public string note;

    public string PrimaryID => id.ToString();
}
