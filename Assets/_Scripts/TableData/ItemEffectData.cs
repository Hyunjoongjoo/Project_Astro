using System;

[Serializable]
public class ItemEffectData : ITableData
{
    public string id;
    public string effectGroupId;
    public EffectType effectType;
    public float effectValue;
    public TriggerCondition triggerCondition;
    public float triggerValue;
    public Target target;
    public string effectDesc;
    public string note;

    public string PrimaryID => id.ToString();
}
