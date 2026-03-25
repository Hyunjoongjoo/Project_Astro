using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxList
{
    HeroDestroySound,
    HeroNormalAttackSound,
    HeroSpawnSound
}
public enum UISfxList
{
    Ping,
    BtnOpen,
    BtnClose
}

[CreateAssetMenu(menuName = "Audio/SFX Table", fileName = "SfxTable")]
public sealed class SFXTable : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public SfxList SfxList;
        public AudioClip clip;
    }
    [Serializable]
    public sealed class UIEntry
    {
        public UISfxList UISfxList;
        public AudioClip clip;
    }

    [Header("Game SFX Settings")]
    [SerializeField] private List<Entry> _entries = new();

    [Header("UI SFX Settings")]
    [SerializeField] private List<UIEntry> _uiEntries = new();

    //캐시 딕셔너리
    private Dictionary<SfxList, AudioClip> _map;
    private Dictionary<UISfxList, AudioClip> _uiMap;

    public bool TryGetClip(SfxList SfxList, out AudioClip clip)
    {
        EnsureCache();
        return _map.TryGetValue(SfxList, out clip);
    }
    public bool TryGetUIClip(UISfxList uiSfx, out AudioClip clip)
    {
        EnsureCache();
        return _uiMap.TryGetValue(uiSfx, out clip);
    }

    private void EnsureCache()
    {
        if (_map != null && _uiMap != null) return;

        _map = new Dictionary<SfxList, AudioClip>(_entries.Count);
        foreach (var e in _entries)
        {
            if (e != null && e.clip != null)
                _map[e.SfxList] = e.clip;
        }

        _uiMap = new Dictionary<UISfxList, AudioClip>(_uiEntries.Count);
        foreach (var e in _uiEntries)
        {
            if (e != null && e.clip != null)
                _uiMap[e.UISfxList] = e.clip;
        }
    }
}
