using Fusion;
using UnityEngine;
using DG.Tweening;

public class SupportSkill : NetworkBehaviour, IHeroSkill
{
    [Header("힐 설정")]
    [SerializeField] private float _healRatio = 0.5f;   // 50%
    [SerializeField] private float _healRange = 5f;

    [Header("이펙트")]
    [SerializeField] private GameObject _healEffectPrefab;
    [SerializeField] private float _effectLifeTime = 0.6f;

    public bool CanUse(HeroController caster)
    {
        return FindHealTarget(caster) != null;
    }

    public bool Execute(HeroController caster)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        UnitBase target = FindHealTarget(caster);
        if (target == null)
        {
            return false;
        }

        float before = target.CurrentHealth;

        caster.HealUnit(target, _healRatio);

        float after = target.CurrentHealth;
        float healed = after - before;

        RPC_PlayHealEffect(target.Object.Id);

        return true;
    }

    private UnitBase FindHealTarget(HeroController caster)
    {
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, _healRange, caster.AllyLayer);

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
    private void RPC_PlayHealEffect(NetworkId targetId)
    {
        if (_healEffectPrefab == null)
        {
            return;
        }

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        GameObject effects = Instantiate(
            _healEffectPrefab,
            targetObj.transform.position,
            Quaternion.identity,
            targetObj.transform //자식으로 부착
        );

        effects.transform.localScale = Vector3.zero;
        effects.transform.DOScale(2f, 0.2f).SetEase(Ease.OutBack);

        Destroy(effects, _effectLifeTime);
    }
}
