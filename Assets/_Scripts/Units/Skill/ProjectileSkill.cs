using Fusion;
using UnityEngine;

public class ProjectileSkill : ISkill
{
    private ProjectileSkillSO _data;
    private bool _isCasting;

    private HeroController _cachedHero;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ProjectileSkill(ProjectileSkillSO data, HeroController hero)
    {
        _data = data;
        _cachedHero = hero;
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ProjectileSkillSO shieldData)
            _data = shieldData;
        else
            Debug.LogWarning($"[ShieldSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        if (_data.projectileVFX == null || _cachedHero.firePoint == null) return false;
        if (_cachedHero.currentTarget == null) return false;

        if ( Vector3.Distance(_cachedHero.transform.position, _cachedHero.currentTarget.transform.position) <= _data.range)
            return true;

        return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        if (_data.projectileVFX == null || _cachedHero.firePoint == null) return;
        if (_cachedHero.currentTarget == null) return;

        Vector3 end = _cachedHero.transform.position;

        RPC_FireProjectile(_cachedHero.team);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FireProjectile(Team team)
    {
        GameObject projectileObj = _cachedHero.InstantiateObject(_data.projectileVFX, _cachedHero.firePoint.position, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(_data, team, _cachedHero.attackPower, _cachedHero.Runner);
            projectile.Fire(_cachedHero.currentTarget.gameObject);
        }
    }
}
