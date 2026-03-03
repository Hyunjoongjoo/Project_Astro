using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HeroIconDataSO", menuName = "Scriptable Objects/HeroIconDataSO")]
public class HeroIconDataSO : ScriptableObject
{
    [Serializable]
    public struct HeroIconInfo
    {
        public string iconId; //테이블 매칭용
        public Sprite iconSprite;
    }

    [SerializeField] private List<HeroIconInfo> _iconList = new List<HeroIconInfo>();
    // 빠른 검색을 위한 딕셔너리 캐싱
    private Dictionary<string, Sprite> _iconCache;

    public Sprite GetIcon(string id)
    {
        if (_iconCache == null)
        {
            _iconCache = new Dictionary<string, Sprite>();
            foreach (var info in _iconList)
            {
                if (!_iconCache.ContainsKey(info.iconId))
                    _iconCache.Add(info.iconId, info.iconSprite);
            }
        }

        if (_iconCache.TryGetValue(id, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[HeroIconDataSO] 아이콘 ID '{id}'를 찾을 수 없습니다.");
        return null;
    }

}
