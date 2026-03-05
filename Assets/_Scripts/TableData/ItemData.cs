using System;

[Serializable]
public class ItemData : ITableData
{
    public string id;
    public string name;
    public string iconImage;
    public string effectGroupId;
    public bool isStackable;
    public string note;

    public string PrimaryID => id.ToString();
}
