using DG.Tweening;
using Fusion;
using UnityEngine;

public class HeroEffectRPC : NetworkBehaviour
{
    [Header("평타,스킬 투사체")]
    [SerializeField] private GameObject _normalProjectile;
    [SerializeField] private GameObject _skillProjectile;

    private GameObject GetProjectilePrefab(ProjectileType type)
    {
        switch (type)
        {
            case ProjectileType.Normal:
                return _normalProjectile;

            case ProjectileType.Skill:
                return _skillProjectile;
        }

        return null;
    }

    //기본 공격
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FireProjectile(
        Vector3 startPos,
        Vector3 targetPos,
        ProjectileType type,
        Team team)
    {
        GameObject prefab = GetProjectilePrefab(type);

        if (prefab == null)
        {
            return;
        }

        GameObject projectileObj = Instantiate(prefab, startPos, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Fire(targetPos, team);
        }
    }


    //스킬 이펙트
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySkillEffect(
        Vector3 pos,
        Quaternion rot,
        float scale,
        float lifeTime,
        bool attachToCaster,
        NetworkId casterId)
    {

        if (!Runner.TryFindObject(casterId, out NetworkObject casterObj))
        {
            return;
        }

        HeroController hero = casterObj.GetComponent<HeroController>();

        if (hero == null)
        {
            return;
        }

        if (hero.SkillData == null)
        {
            return;
        }

        GameObject prefab = hero.SkillData.EffectPrefab;

        if (prefab == null)
        {
            return;
        }

        GameObject fx;

        if (attachToCaster)
        {
            fx = Instantiate(prefab, hero.transform);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
        }
        else
        {
            fx = Instantiate(prefab, pos, rot);
        }

        fx.transform.localScale = Vector3.one * scale;

        Destroy(fx, lifeTime);
    }


    //힐 이펙트
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHealEffect(
        NetworkId targetId,
        float scale,
        float lifeTime)
    {

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        HeroController hero = targetObj.GetComponent<HeroController>();

        if (hero == null || hero.SkillData == null)
        {
            return;
        }

        GameObject prefab = hero.SkillData.EffectPrefab;

        if (prefab == null)
        {
            return;
        }

        GameObject effects = Instantiate(prefab, targetObj.transform.position, Quaternion.identity, targetObj.transform);

        effects.transform.localScale = Vector3.zero;

        effects.transform
            .DOScale(scale, 0.5f)
            .SetEase(Ease.OutBack);

        Destroy(effects, lifeTime);
    }
}
