using UnityEngine;
using System.Collections.Generic;

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
