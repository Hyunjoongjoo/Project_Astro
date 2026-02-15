using System;

[Serializable]
public class ItemEffectData : ITableData
{
    public string id;
    public string effectGroupId;
    public int effectType;
    public string effectValue;
    public int triggerCondition;
    public string triggerValue;
    public int target;
    public string note;
    public string note2;

    public string PrimaryID => id.ToString();
}
