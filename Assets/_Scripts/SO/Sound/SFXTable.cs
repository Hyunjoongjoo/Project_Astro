using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxList
{
    HeroDestroySound,
    HeroNormalAttackSound,
    HeroSpawnSound
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

    [SerializeField] private List<Entry> _entries = new();

    private Dictionary<SfxList, AudioClip> _map;

    public bool TryGetClip(SfxList SfxList, out AudioClip clip)
    {
        EnsureCache();
        return _map.TryGetValue(SfxList, out clip);
    }

    private void EnsureCache()
    {
        if (_map != null) return;

        _map = new Dictionary<SfxList, AudioClip>(_entries.Count);
        foreach (var e in _entries)
        {
            if (e != null && e.clip != null)
                _map[e.SfxList] = e.clip;
        }
    }
}
