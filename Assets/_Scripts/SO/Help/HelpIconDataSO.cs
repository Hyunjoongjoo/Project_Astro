using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HelpIconDataSO", menuName = "Scriptable Objects/HelpIconDataSO")]
public class HelpIconDataSO : ScriptableObject
{
    [System.Serializable]
    public class HelpIconMap
    {
        public string id;      // CSV의 이미지에 적힌 스트링
        public Sprite sprite;  // 실제 매칭될 이미지 파일
    }

    [SerializeField] private List<HelpIconMap> _iconMaps = new List<HelpIconMap>();

    // 검색 성능을 위한 딕셔너리 캐싱
    private Dictionary<string, Sprite> _iconDict;

    public Sprite GetIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[HelpIconDataSO] 전달된 ID가 Null 혹은 Empty입니다!");
            return null;
        }

        if (_iconDict == null)
        {
            _iconDict = new Dictionary<string, Sprite>();
            foreach (var map in _iconMaps)
            {
                if (!_iconDict.ContainsKey(map.id))
                {
                    _iconDict.Add(map.id, map.sprite);
                }
            }
        }

        if (_iconDict.TryGetValue(id, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[HelpIconDataSO] 해당 ID의 아이콘을 찾을 수 없습니다: {id}");
        return null;
    }
}
