using UnityEngine;
using System.Collections.Generic;

// 미니언에게 적용하는 히어로 매니저 같은 클래스
// 지금은 안쓰고 있음.
// 그런데 게임이 지속되며 미니언의 스펙이 점점 상승하는 방식이거나
// 활용될 여지가 있다면 이 클래스를 수정

public class UnitManager : Singleton<UnitManager>
{
    // 유닛 데이터 캐싱
    private Dictionary<string, UnitData> _unitDataCache = new Dictionary<string, UnitData>();


    public void Init()
    {
        var table = TableManager.Instance.UnitTable;

        if (table == null)
        {
            Debug.LogError("UnitTable이 없습니다.");
            return;
        }

        foreach (var data in table.GetAll())
        {
            if (!_unitDataCache.ContainsKey(data.id))
            {
                _unitDataCache.Add(data.id, data);
            }
        }

        Debug.Log($"[UnitManager] 유닛 데이터 캐싱 완료 : {_unitDataCache.Count}");
    }


    public UnitData GetUnitData(string unitId)
    {
        if (_unitDataCache.TryGetValue(unitId, out var data))
        {
            return data;
        }

        Debug.LogWarning($"UnitManager: 유닛 데이터 없음 {unitId}");
        return null;
    }
}
