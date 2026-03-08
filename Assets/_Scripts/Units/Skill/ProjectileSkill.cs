using Fusion;
using UnityEngine;

public class ProjectileSkill : ISkill
{
    private ProjectileSkillSO _data;
    private bool _isCasting;

    private MinionController _cachedUnit;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ProjectileSkill(ProjectileSkillSO data, MinionController unit)
    {
        _data = data;
        _cachedUnit = unit;
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
        if (_data.projectileVFX == null || _cachedUnit.firePoint == null) return false;
        if (_cachedUnit.currentTarget == null) return false;

        if ( Vector3.Distance(_cachedUnit.transform.position, _cachedUnit.currentTarget.transform.position) <= _data.range)
            return true;

        return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        if (_data.projectileVFX == null || _cachedUnit.firePoint == null) return;
        if (_cachedUnit.currentTarget == null) return;

        Vector3 end = _cachedUnit.transform.position;

        RPC_FireProjectile(_cachedUnit.team);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FireProjectile(Team team)
    {
        GameObject projectileObj = _cachedUnit.InstantiateObject(_data.projectileVFX, _cachedUnit.firePoint.position, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(_data, team, _cachedUnit.AttackPower, _cachedUnit.Runner);
            projectile.Fire(_cachedUnit.currentTarget.gameObject);
        }
    }
}
