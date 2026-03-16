using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroIconDataSO", menuName = "Scriptable Objects/HeroIconDataSO")]
public class HeroIconDataSO : ScriptableObject
{
    [Serializable]
    public struct HeroIconInfo
    {
        public string heroId; //테이블 매칭용
        public Sprite iconSprite; // 영웅 초상화 아이콘
        public List<BaseSkillSO> skillIcons; //스킬 아이콘 리스트
        public NetworkPrefabRef heroPrefab; //소환용 네트워크 프리팹 추가
    }

    [SerializeField] private List<HeroIconInfo> _iconList = new List<HeroIconInfo>();

    // 빠른 검색을 위한 딕셔너리 캐싱
    private Dictionary<string, Sprite> _iconCache;
    private Dictionary<string, List<BaseSkillSO>> _skillIconCache;

    //3.3 여현구 프리팹캐싱
    private Dictionary<string, NetworkPrefabRef> _prefabCache;

    public Sprite GetIcon(string id)
    {
        if (_iconCache == null)
        {
            CacheData();
        }

        if (_iconCache.TryGetValue(id, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[HeroIconDataSO] 아이콘 ID '{id}'를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 해당 영웅의 모든 스킬 아이콘 리스트를 반환
    /// </summary>
    public List<BaseSkillSO> GetSkillIcons(string id)
    {
        if (_skillIconCache == null) CacheData();
        return _skillIconCache.TryGetValue(id, out List<BaseSkillSO> icons) ? icons : null;
    }

    //3.3 여현구 프리팹받아오는 메서드
    public NetworkPrefabRef GetPrefab(string id)
    {
        if (_prefabCache == null) CacheData();

        if (_prefabCache.TryGetValue(id, out NetworkPrefabRef prefab))
            return prefab;

        Debug.LogWarning($"프리팹 아이디 못 찾음");
        return default; 
    }

    private void CacheData()
    {
        _iconCache = new Dictionary<string, Sprite>();
        _prefabCache = new Dictionary<string, NetworkPrefabRef>();
        _skillIconCache = new Dictionary<string, List<BaseSkillSO>>();

        foreach (var info in _iconList)
        {
            if (string.IsNullOrEmpty(info.heroId)) continue;

            if (!_iconCache.ContainsKey(info.heroId))
                _iconCache.Add(info.heroId, info.iconSprite);

            if (!_prefabCache.ContainsKey(info.heroId))
                _prefabCache.Add(info.heroId, info.heroPrefab);

            if (!_skillIconCache.ContainsKey(info.heroId))
                _skillIconCache.Add(info.heroId, info.skillIcons);
        }
    }

}
