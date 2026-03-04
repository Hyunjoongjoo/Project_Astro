using DG.Tweening;
using Fusion;
using UnityEngine;



public class AngelSkill : MonoBehaviour, IHeroSkill
{
    [SerializeField] private AngelSkillSO _data;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        if (!runtime.IsAreaSkill)
        {
            return FindHealTarget(caster, runtime) != null;
        }
        else
        {
            // 범위 힐일 경우 영웅or미니언 중 한 명이라도 힐 가능한지 검사
            Collider[] hits = Physics.OverlapSphere(
                caster.transform.position,
                runtime.Radius,
                caster.AllyLayer
            );

            foreach (var hit in hits)
            {
                UnitBase unit = hit.GetComponentInParent<UnitBase>();
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                if (unit.UnitType != UnitType.Hero && unit.UnitType != UnitType.Minion)
                {
                    continue;
                }

                if (unit.CurrentHealth < unit.MaxHealth)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        Debug.Log($"[Skill Execute] {caster.name} skill 실행 | EffectPrefab: {runtime.EffectPrefab}");
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        if (!runtime.IsAreaSkill)
        {
            //기존 단일 힐
            UnitBase target = FindHealTarget(caster, runtime);
            if (target == null)
            {
                return false;
            }

            caster.HealUnit(target, runtime.HealAmount);
            caster.RPC_PlayHealEffect(target.Object.Id, SkillEffectType.Angel, runtime.EffectLifeTime);
        }
        else
        {
            //범위 힐
            Collider[] hits = Physics.OverlapSphere(
                caster.transform.position,
                runtime.Radius,
                caster.AllyLayer
            );

            bool healedAnyone = false;

            foreach (var hit in hits)
            {
                UnitBase unit = hit.GetComponent<UnitBase>();
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                if (unit.CurrentHealth >= unit.MaxHealth)
                {
                    continue;
                }

                if (unit.UnitType != UnitType.Hero && unit.UnitType != UnitType.Minion)
                {
                    continue;
                }

                //if (unit == caster) //본인 포함 여부에 따라 본인제외시 추가
                //{
                //    continue;
                //}

                caster.HealUnit(unit, runtime.HealAmount);
                caster.RPC_PlayHealEffect(unit.Object.Id, SkillEffectType.Angel, runtime.EffectLifeTime);
                healedAnyone = true;
            }

            if (!healedAnyone)
            {
                return false;
            }
        }

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is AngelSkillSO supportData)
        {
            _data = supportData;
        }
    }

    private UnitBase FindHealTarget(HeroController caster, SkillRuntimeData runtime)
    {
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, runtime.SkillRange, caster.AllyLayer);

        UnitBase best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }

            if (unit.UnitType != UnitType.Hero)
            {
                continue;
            }

            if (unit == caster)
            {
                continue;
            }

            if (unit.IsDead)
            {
                continue;
            }

            if (unit.CurrentHealth >= unit.MaxHealth)
            {
                continue;
            }

            float dist = Vector3.Distance(caster.transform.position, unit.transform.position);

            if (dist < bestDist)
            {
                best = unit;
                bestDist = dist;
            }
        }

        return best;
    }
}
