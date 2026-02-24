using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StatIconData
{
    public StatType statType;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "StatIconDataSO", menuName = "Scriptable Objects/StatIconDataSO")]
public class StatIconDataSO : ScriptableObject
{
    [SerializeField] private List<StatIconData> _iconList;

    // 런타임에서 빠르게 찾기 위한 Dictionary
    private Dictionary<StatType, Sprite> _iconDict;

    public void Initialize()
    {
        if (_iconDict != null) return;

        _iconDict = new Dictionary<StatType, Sprite>();
        foreach (var data in _iconList)
        {
            if (!_iconDict.ContainsKey(data.statType))
                _iconDict.Add(data.statType, data.icon);
        }
    }

    public Sprite GetIcon(StatType type)
    {
        if (_iconDict == null) Initialize();

        if (_iconDict.TryGetValue(type, out Sprite icon))
            return icon;

        Debug.LogWarning($"{type}에 해당하는 아이콘이 SO에 없습니다!");
        return null;
    }
}
