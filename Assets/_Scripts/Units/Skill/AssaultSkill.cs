using Fusion;
using UnityEngine;
using DG.Tweening;

public class AssaultSkill : NetworkBehaviour, IHeroSkill
{
    [SerializeField] private AssaultSkillSO _data;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        if (caster.CurrentTarget == null)
        {
            return false;
        }

        float dist = caster.GetAttackDistanceTo(caster.CurrentTarget);

        return dist <= _data.SkillRange;//기획서에 맞게 스킬범위를 새로 지정
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        UnitBase target = GetAssaultBaseTarget(caster);
        if (target == null || target.IsDead)
        {
            return false;
        }

        //워프 위치 계산 (타겟 정중앙)
        Vector3 warpPos = target.transform.position;
        warpPos.y = caster.transform.position.y; //지면 보정

        //워프
        caster.transform.position = warpPos;
        caster.ForceStopMoveForSkill();

        LayerMask targetMask = caster.TargetLayer;

        Collider[] hits = Physics.OverlapSphere(warpPos, runtime.Radius, targetMask);

        foreach (var hit in hits)
        {
            UnitBase unit = hit.GetComponent<UnitBase>();
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            float damage = caster.AttackPower * runtime.DamageMultiplier;
            unit.TakeDamage(damage);
        }

        //이펙트
        RPC_PlayEffect(caster.Object.Id);

        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffect(NetworkId casterId)
    {
        if (!Runner.TryFindObject(casterId, out NetworkObject casterObj))
        {
            return;
        }

        PlayEffect(casterObj.transform);
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is AssaultSkillSO assaultData)
        {
            _data = assaultData;
        }
    }

    private UnitBase GetAssaultBaseTarget(HeroController caster)
    {
        //현재 전투 타겟 우선
        if (caster.CurrentTarget != null && !caster.CurrentTarget.IsDead)
        {
            return caster.CurrentTarget;
        }

        return null;
    }

    private void PlayEffect(Transform casterTransform)
    {
        if (_data.EffectPrefab == null)
        {
            return;
        }

        GameObject effects = Instantiate(
         _data.EffectPrefab,
         casterTransform.position,
         Quaternion.identity,
         casterTransform
     );

        effects.transform.localPosition = Vector3.zero;

        ParticleSystem ps = effects.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        effects.transform.localScale = Vector3.zero;
        effects.transform.DOScale(_data.Radius * 2f * 6.5f, _data.EffectLifeTime).SetEase(Ease.OutQuad);

        float lifeTime = ps != null
            ? ps.main.duration + ps.main.startLifetime.constantMax
            : 1f;

        Destroy(effects, lifeTime);
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.Radius);
    }
#endif
}
