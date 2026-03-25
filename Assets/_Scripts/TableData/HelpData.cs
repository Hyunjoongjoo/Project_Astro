using System;

[Serializable]
public class HelpData : ITableData
{
    public string id;
    public Category category;
    public int page;
    public string image;
    public string des;
    public string note;

    public string PrimaryID => id.ToString();
}
