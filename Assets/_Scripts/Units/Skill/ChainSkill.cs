using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ChainSkill : BaseSkill<ChainSkillSO>
{
    
    private HashSet<UnitBase> _visited = new HashSet<UnitBase>();
    private Collider[] _overlapResults = new Collider[20];

    //디버그용(기획 확인용)
    public List<UnitBase> debugChainTargets = new List<UnitBase>();

    public ChainSkill(ChainSkillSO data, UnitController unit) : base(data, unit)
    {
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);
    }

    public override void ChangeData(BaseSkillSO newData)
    {
        if (newData is ChainSkillSO chainData)
            _data = chainData;
        else
            Debug.LogWarning($"[체인스킬] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public override bool UsingConditionCheck()
    {
        if (!_skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner)) return false;
        if (_data.heroOnly)
        {
            UnitBase hero = _cachedUnit.FindOnlyHeroTarget(_overlapResults, _data.range);

            if (hero == null)
            {
                return false;
            }

            Collider myCol = _cachedUnit.GetComponent<Collider>();
            Collider heroCol = hero.GetComponent<Collider>();

            float sqrDist;

            if (myCol == null || heroCol == null)
            {
                sqrDist = (_cachedUnit.transform.position - hero.transform.position).sqrMagnitude;
            }
            else
            {
                Vector3 myPoint = myCol.ClosestPoint(hero.transform.position);
                Vector3 heroPoint = heroCol.ClosestPoint(myCol.transform.position);

                sqrDist = (myPoint - heroPoint).sqrMagnitude;
            }

            return sqrDist <= _data.range * _data.range;
        }

        if (_cachedUnit.currentTarget == null) return false;
        if (_cachedUnit.currentTarget.IsDead) return false;

        Collider myCol2 = _cachedUnit.GetComponent<Collider>();
        Collider targetCol = _cachedUnit.currentTarget.GetComponent<Collider>();

        float sqrDist2;

        if (myCol2 == null || targetCol == null)
        {
            sqrDist2 = (_cachedUnit.transform.position - _cachedUnit.currentTarget.transform.position).sqrMagnitude;
        }
        else
        {
            Vector3 myPoint = myCol2.ClosestPoint(targetCol.transform.position);
            Vector3 targetPoint = targetCol.ClosestPoint(myCol2.transform.position);

            sqrDist2 = (myPoint - targetPoint).sqrMagnitude;
        }

        return sqrDist2 <= _data.range * _data.range;
    }

    public override void PreDelay()
    {
        _phase = SkillPhase.PreDelay;
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.preDelay);
    }

    public override void PostDelay()
    {
        
    }

    public override void Casting()
    {
        if (!_cachedUnit.Object.HasStateAuthority) return;

        UnitBase firstTarget = null;

        if (_data.heroOnly)
        {
            firstTarget = _cachedUnit.FindOnlyHeroTarget(_overlapResults, _data.range);
        }
        else
        {
            firstTarget = _cachedUnit.currentTarget;
        }

        if (firstTarget == null) return;
        if (firstTarget.IsDead) return;
        if (firstTarget.Object == null) return;

        _phase = SkillPhase.Casting;

        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);

        _visited.Clear();
        debugChainTargets.Clear();

        Chain(firstTarget, null, 0);

    }

    public override void Tick()
    {
        switch (_phase)
        {
            case SkillPhase.PreDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    Casting();
                break;

            case SkillPhase.Casting:
                _phase = SkillPhase.PostDelay;
                _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.postDelay);
                break;

            case SkillPhase.PostDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    _phase = SkillPhase.Idle;
                break;
        }
    }

    private void Chain(UnitBase current, UnitBase prev, int chainCount)
    {
        if (current == null || current.IsDead) return;

        debugChainTargets.Add(current);

        bool isChained = chainCount > 0;

        ApplyDamage(current, isChained, chainCount);

        Vector3 startPos = prev != null ? prev.transform.position : _cachedUnit.transform.position;
        Vector3 endPos = current.transform.position;

        if (_cachedUnit.HasStateAuthority && current.Object != null)
        {
            NetworkId fromId = (prev != null) ? prev.Object.Id : _cachedUnit.Object.Id;

            _cachedUnit.RPC_PlayChainEffect(fromId, current.Object.Id);
        }

        _visited.Add(current);

        if (chainCount >= _data.maxChainCount) return;      

        UnitBase next = FindNextTarget(current);

        if (next == null) return;

        Vector3 start = current.transform.position;
        Vector3 dir = next.transform.position - start;

        Chain(next, current, chainCount + 1);
    }

    private void ApplyDamage(UnitBase target, bool isChained, int chainCount)
    {
        float damage = _cachedUnit.AttackPower * _data.damageRatio; //기본공격력*비율
        if (isChained) damage *= _data.chainDamageMultiplier; //1회만 감소    
        Debug.Log($"[체인스킬] HIT[{chainCount}] → {isChained} / dmg:{damage}");
        if (_cachedUnit.HasStateAuthority) target.TakeDamage(damage);
    }

    // === 이 메서드는 유닛 컨트롤러로 옮김. 동작 문제 없으면 제거 ===

    //public UnitBase FindNearestHeroTarget()// 주변에서 가장 가까운 적 영웅 탐색
    //{
    //    int hitCount = Physics.OverlapSphereNonAlloc(
    //        _cachedUnit.transform.position,
    //        _data.range,
    //        _overlapResults,
    //        _cachedUnit.TargetLayer
    //    );

    //    UnitBase closestHero = null;
    //    float minSqrDist = float.MaxValue;

    //    for (int i = 0; i < hitCount; i++)
    //    {
    //        Collider overlapCollider = _overlapResults[i];
    //        if (overlapCollider == null) continue;
    //        if (!overlapCollider.TryGetComponent(out UnitBase targetUnit)) continue;          
    //        if (targetUnit == _cachedUnit) continue;           
    //        if (targetUnit.IsDead)  continue;           
    //        if (targetUnit.Object == null) continue;
    //        if (targetUnit.team == _cachedUnit.team) continue;          
    //        if (targetUnit.UnitType != UnitType.Hero) continue;            

    //        float sqrDist = (_cachedUnit.transform.position - targetUnit.transform.position).sqrMagnitude;

    //        if (sqrDist < minSqrDist)
    //        {
    //            minSqrDist = sqrDist;
    //            closestHero = targetUnit;
    //        }
    //    }

    //    return closestHero;
    //}

    private UnitBase FindNextTarget(UnitBase current)// 다음 체인 대상 탐색 (영웅 우선 없으면 다른 유닛)
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

            //영웅 우선 없으면 아무나(이미 맞은 유닛 제외)
            if (unit.UnitType == UnitType.Hero)
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

        return closestHero != null ? closestHero : closestOther;
    }

}
