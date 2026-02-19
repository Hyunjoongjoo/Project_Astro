using System;

[Serializable]
public class ConfigData : ITableData
{
    public String id;
    public String configValue;
    public String note;

    public string PrimaryID => id.ToString();
}
