using System;

[Serializable]
public class ItemEffectData : ITableData
{
    public String id;
    public String effectGroupId;
    public EffectType effectType;
    public String effectValue;
    public TriggerCondition triggerCondition;
    public String triggerValue;
    public Target target;
    public String effectDesc;
    public String note;

    public string PrimaryID => id.ToString();
}
