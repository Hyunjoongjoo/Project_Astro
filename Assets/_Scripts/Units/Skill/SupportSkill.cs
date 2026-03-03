using DG.Tweening;
using Fusion;
using UnityEngine;


public class SupportSkill : NetworkBehaviour, IHeroSkill
{
    [SerializeField] private SupportSkillSO _data;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        return FindHealTarget(caster, runtime) != null;
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        UnitBase target = FindHealTarget(caster, runtime);
        if (target == null)
        {
            return false;
        }

        float before = target.CurrentHealth;

        caster.HealUnit(target, runtime.HealAmount);

        float after = target.CurrentHealth;
        float healed = after - before;

        RPC_PlayHealEffect(target.Object.Id, runtime.EffectLifeTime);

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is SupportSkillSO supportData)
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHealEffect(NetworkId targetId, float effectLifeTime)
    {
        if (_data.EffectPrefab == null)
        {
            return;
        }

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        GameObject effects = Instantiate(
            _data.EffectPrefab,
            targetObj.transform.position,
            Quaternion.identity,
            targetObj.transform //자식으로 부착
        );

        effects.transform.localScale = Vector3.zero;
        effects.transform.DOScale(2f, 0.2f).SetEase(Ease.OutBack);

        Destroy(effects, _data.Duration);
    }
}
