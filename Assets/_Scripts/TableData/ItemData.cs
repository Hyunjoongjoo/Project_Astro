using System;

[Serializable]
public class ItemData : ITableData
{
    public String id;
    public String name;
    public ItemType itemType;
    public String iconImage;
    public String effectGroupId;
    public bool isStackable;
    public String note;

    public string PrimaryID => id.ToString();
}
