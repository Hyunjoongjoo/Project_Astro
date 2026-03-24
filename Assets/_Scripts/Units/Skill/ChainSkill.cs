using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ChainSkill : ISkill
{
    private ChainSkillSO _data;
    private UnitController _cachedUnit;
    private bool _isCasting;
    private TickTimer _skillCooldown;

    private HashSet<UnitBase> _visited = new HashSet<UnitBase>();
    private Collider[] _overlapResults = new Collider[20];

    //디버그용(기획 확인용)
    public List<UnitBase> debugChainTargets = new List<UnitBase>();

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ChainSkill(ChainSkillSO data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ChainSkillSO chainData)
            _data = chainData;
        else
            Debug.LogWarning($"[체인스킬] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;
        if (_cachedUnit.currentTarget == null) return false;

        float sqrDist = (_cachedUnit.transform.position - _cachedUnit.currentTarget.transform.position).sqrMagnitude;

        return sqrDist <= _data.range * _data.range;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        if (!_cachedUnit.Object.HasStateAuthority) return;
        if (_cachedUnit.currentTarget == null) return;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);
        _visited.Clear();
        debugChainTargets.Clear();
        _visited.Add(_cachedUnit.currentTarget);
        Debug.Log($"[체인스킬] 시작 위치: {_cachedUnit.currentTarget.name}");
        Chain(_cachedUnit.currentTarget, null, 0);

    }

    private void Chain(UnitBase current, UnitBase prev, int chainCount)
    {
        if (prev != null)//첫타격
        {
            Debug.DrawRay(
                prev.transform.position,
                current.transform.position - prev.transform.position,
                Color.cyan,
                0.3f
            );
        }

        if (current == null || current.IsDead) return;

        debugChainTargets.Add(current);

        bool isChained = chainCount > 0;

        ApplyDamage(current, isChained);

        //추후에 이펙트 추가 예정

        _visited.Add(current);

        Debug.Log($"[체인스킬] 타겟: {current.name} | Count: {chainCount}");

        if (chainCount >= _data.maxChainCount) return;      

        UnitBase next = FindNextTarget(current);

        if (next == null) return;

        Vector3 start = current.transform.position;
        Vector3 dir = next.transform.position - start;

        Debug.DrawRay(start, dir, Color.yellow, 0.3f);


        Chain(next, current, chainCount + 1);
    }

    private void ApplyDamage(UnitBase target, bool isChained)
    {
        float damage = _cachedUnit.AttackPower * _data.damageRatio; //기본공격력*비율

        if (isChained) damage *= _data.chainDamageMultiplier; //1회만 감소    
        if (_cachedUnit.Runner.IsSharedModeMasterClient) target.TakeDamage(damage);
    }

    private UnitBase FindNextTarget(UnitBase current)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
           current.transform.position,
           _data.chainRange,
           _overlapResults,
           _cachedUnit.TargetLayer
       );

        UnitBase closestHero = null;
        float minHeroDist = float.MaxValue;

        UnitBase closestOther = null;
        float minOtherDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _overlapResults[i];

            if (!col.TryGetComponent(out UnitBase unit)) continue;
            if (unit == _cachedUnit) continue;
            if (unit.IsDead) continue;
            if (_visited.Contains(unit)) continue;
            if (unit.team == _cachedUnit.team) continue;            

            float sqrDist = (current.transform.position - unit.transform.position).sqrMagnitude;

            if (_data.heroOnly)//heroOnly = true
            {
                if (unit.UnitType != UnitType.Hero) continue;

                if (sqrDist < minHeroDist)
                {
                    minHeroDist = sqrDist;
                    closestHero = unit;
                }
            }
            else//heroOnly false
            {
                if (unit.UnitType == UnitType.Hero)//영웅 우선 분리
                {
                    if (sqrDist < minHeroDist)
                    {
                        minHeroDist = sqrDist;
                        closestHero = unit;
                    }
                }
                else
                {
                    if (sqrDist < minOtherDist)
                    {
                        minOtherDist = sqrDist;
                        closestOther = unit;
                    }
                }
            }
        }

        //heroOnly true : 영웅만
        if (_data.heroOnly) return closestHero;

        //heroOnly false : 영웅 우선
        return closestHero != null ? closestHero : closestOther;
    }


}
