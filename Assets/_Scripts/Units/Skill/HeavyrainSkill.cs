using Fusion;
using UnityEngine;

public class HeavyrainSkill : NetworkBehaviour, IHeroSkill
{
    [SerializeField] private HeavyrainSkillSO _data;

    private TickTimer _shotTimer;
    private int _remainingShots;

    private HeroController _caster;
    private UnitBase _target;
    private bool _isFiring;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        if (caster == null)
        {
            return false;
        }

        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        if (_isFiring)
        {
            return false;
        }

        UnitBase target = caster.CurrentTarget;
        if (target == null || target.IsDead)
        {
            return false;
        }

        float dist = caster.GetAttackDistanceTo(target);

        return dist <= _data.SkillRange;
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        if (!CanUse(caster, runtime))
        {
            return false;
        }

        _caster = caster;
        _target = caster.CurrentTarget;

        _remainingShots = runtime.ShotCount;
        _isFiring = true;

        FireOnce();

        if (_remainingShots > 0)
        {
            _shotTimer = TickTimer.CreateFromSeconds(Runner, runtime.ShotInterval);
        }
        else
        {
            StopFiring();
        }

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is HeavyrainSkillSO barrageData)
        {
            _data = barrageData;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isFiring)
        {
            return;
        }

        if (!_shotTimer.IsRunning || !_shotTimer.Expired(Runner))
        {
            return;
        }

        if (_remainingShots <= 0 ||
            _caster == null ||
            _target == null ||
            _target.IsDead)
        {
            StopFiring();
            return;
        }

        FireOnce();

        if (_remainingShots > 0)
        {
            _shotTimer = TickTimer.CreateFromSeconds(Runner, _data.ShotInterval);
        }
        else
        {
            StopFiring();
        }
    }

    //데미지는 영웅컨트롤러로 위임, 연출만 RPC
    private void FireOnce()
    {
        _remainingShots--;

        _caster.ApplyBarrageSkillDamage(_target, _data.DamageMultiplier);

        if (_data.EffectPrefab != null)
        {
            RPC_FireProjectile(_caster.Object.Id, _target.Object.Id, _caster.team);
        }
    }

    // 포격 종료 처리
    private void StopFiring()
    {
        _shotTimer = TickTimer.None;
        _remainingShots = 0;
        _target = null;
        _caster = null;
        _isFiring = false;
    }

    //투사체 연출
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FireProjectile(NetworkId casterId, NetworkId targetId, Team team)
    {
        if (!Runner.TryFindObject(casterId, out NetworkObject casterObj))
        {
            return;
        }

        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            return;
        }

        Vector3 start = casterObj.transform.position;
        Vector3 end = targetObj.transform.position;

        GameObject projectileObj = Instantiate(
            _data.EffectPrefab,
            start,
            Quaternion.identity
        );

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Fire(end, team);
        }
    }
}
