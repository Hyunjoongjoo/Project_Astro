using UnityEngine;
using System.Collections.Generic;

public class HpBarManager : Singleton<HpBarManager>
{
    [SerializeField] private string _hpBarPoolTag = "HPBar";
    [SerializeField] private Transform _hpBarContainer; 

    // hp바들 관리용
    private Dictionary<Transform, HpBarItem> _activeBars = new Dictionary<Transform, HpBarItem>();


    //데미지 받을 때 호출시킬 메서드
    public void OnUnitDamaged(Transform unitTransform, Team team, float currentHp, float maxHp, UnitType unitType)
    {
        float ratio = currentHp / maxHp;
        bool isHero = (unitType == UnitType.Hero); //영웅인지 체크

        // 이미 떠있으면 갱신
        if (_activeBars.TryGetValue(unitTransform, out HpBarItem bar))
        {
            if (bar.gameObject.activeSelf)
            {
                bar.UpdateHp(ratio);
                return;
            }
            else
            {
                _activeBars.Remove(unitTransform);
            }
        }

        // 없으면 풀에서 꺼내쓰기
        GameObject obj = PoolManager.Instance.SpawnFromPool(_hpBarPoolTag, Vector3.zero, Quaternion.identity, _hpBarContainer);
        if (obj.TryGetComponent(out HpBarItem newBar))
        {
            newBar.Setup(unitTransform, team, ratio, _hpBarPoolTag,isHero);
            _activeBars[unitTransform] = newBar;
        }
    }
}
