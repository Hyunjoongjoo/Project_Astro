using System;

[Serializable]
public class ConfigData : ITableData
{
    public string id;
    public string configValue;
    public string note;

    public string PrimaryID => id.ToString();
}
