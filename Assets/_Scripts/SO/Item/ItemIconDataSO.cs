using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemIconDataSO", menuName = "Scriptable Objects/ItemIconDataSO")]
public class ItemIconDataSO : ScriptableObject
{
    [Serializable]
    public struct ItemIconInfo
    {
        public string itemId;     //csvId
        public Sprite iconSprite; 
    }

    [SerializeField] private List<ItemIconInfo> _iconList = new List<ItemIconInfo>();

    //캐싱
    private Dictionary<string, Sprite> _iconCache;

    public Sprite GetIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_iconCache == null)
        {
            CacheData();
        }

        if (_iconCache.TryGetValue(id, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"아이템 ID '{id}'연결 확인 필요");
        return null;
    }

    private void CacheData()
    {
        _iconCache = new Dictionary<string, Sprite>();

        foreach (var info in _iconList)
        {
            if (string.IsNullOrEmpty(info.itemId)) continue;

            if (!_iconCache.ContainsKey(info.itemId))
            {
                _iconCache.Add(info.itemId, info.iconSprite);
            }
        }
    }
}