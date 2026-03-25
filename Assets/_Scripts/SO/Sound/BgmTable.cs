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
        public List<AudioClip> clip = new ();
    }

    [SerializeField] private List<Entry> _entries = new();

    private Dictionary<SceneState, List<AudioClip>> _map;

    public bool TryGetClip(SceneState state, out AudioClip clip)
    {
        clip = null;
        EnsureCache();

        if (_map.TryGetValue(state, out var list) && list.Count > 0)
        {
            // 리스트 중 랜덤으로 하나를 선택하여 반환
            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            clip = list[randomIndex];
            return true;
        }
        return false;
    }

    private void EnsureCache()
    {
        if (_map != null) return;

        _map = new Dictionary<SceneState, List<AudioClip>>(_entries.Count);
        foreach (var e in _entries)
        {
            if (e != null && e.clip != null)
                _map[e.state] = e.clip;
        }
    }
}
