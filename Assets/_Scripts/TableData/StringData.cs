using System;

[Serializable]
public class StringData : ITableData
{
    public string id;
    public string textKor;
    public string note;

    public string PrimaryID => id.ToString();
}
