using System;

[Serializable]
public class ItemEffectData : ITableData
{
    public string id;
    public string effectGroupId;
    public EffectType effectType;
    public string effectValue;
    public TriggerCondition triggerCondition;
    public string triggerValue;
    public Target target;
    public string effectDesc;
    public string note;

    public string PrimaryID => id.ToString();
}
