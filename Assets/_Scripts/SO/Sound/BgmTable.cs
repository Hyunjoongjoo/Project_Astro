using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/BGM Table", fileName = "BgmTable")]
public sealed class BgmTable : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public SceneState state;
        public AudioClip clip;
    }

    [SerializeField] private List<Entry> _entries = new();

    private Dictionary<SceneState, AudioClip> _map;

    public bool TryGetClip(SceneState state, out AudioClip clip)
    {
        EnsureCache();
        return _map.TryGetValue(state, out clip);
    }

    private void EnsureCache()
    {
        if (_map != null) return;

        _map = new Dictionary<SceneState, AudioClip>(_entries.Count);
        foreach (var e in _entries)
        {
            if (e != null && e.clip != null)
                _map[e.state] = e.clip;
        }
    }
}
