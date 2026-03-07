using Fusion;
using UnityEngine;

public class HeavyrainSkill : MonoBehaviour, IHeroSkill
{
    [SerializeField] private HeavyrainSkillSO _data;

    private TickTimer _shotTimer;
    private int _remainingShots;
    private HeroController _caster;
    private UnitBase _target;
    private bool _isFiring;
    private SkillRuntimeData _runtime;

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

        return dist <= runtime.SkillRange;
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
    {
        if (!CanUse(caster, runtime))
        {
            return false;
        }

        UnitBase target = caster.CurrentTarget;

        if (target == null || target.IsDead)
        {
            return false;
        }

        _caster = caster;
        _target = target;

        _runtime = runtime;

        _remainingShots = runtime.ShotCount;
        _isFiring = true;

        //첫 발 즉시 발사
        FireOnce(runtime);

        if (_remainingShots > 0)
        {
            _shotTimer = TickTimer.CreateFromSeconds(caster.Runner, runtime.ShotInterval);
        }
        else
        {
            StopFiring();
        }

        return true;
    }

    public void TickSkill(NetworkRunner runner)
    {
        if (_caster == null)
        {
            return;
        }

        if (!_caster.Object.HasStateAuthority)
        {
            return;
        }

        if (!_isFiring)
        {
            return;
        }

        if (_target == null || _target.IsDead || _target.Object == null)
        {
            StopFiring();
            return;
        }

        if (!_shotTimer.ExpiredOrNotRunning(runner))
        {
            return;
        }

        if (_remainingShots <= 0)
        {
            StopFiring();
            return;
        }


        FireOnce(_runtime);

        _shotTimer = TickTimer.CreateFromSeconds(runner, _runtime.ShotInterval);
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is HeavyrainSkillSO barrageData)
        {
            _data = barrageData;
        }
    }

    private void FireOnce(SkillRuntimeData runtime)
    {
        if (_caster == null || _target == null)
        {
            StopFiring();
            return;
        }

        if (_target.IsDead || _target.Object == null)
        {
            StopFiring();
            return;
        }

        _remainingShots--;

        //모든 클라이언트에 발사 연출
        //_caster.RPC_FireProjectile(_caster.Object.Id, _target.Object.Id, _caster.team, true);

        //서버에서 데미지 적용
        _caster.ApplyBarrageSkillDamage(_target, runtime.DamageMultiplier);

    }

    private void StopFiring()
    {
        _shotTimer = TickTimer.None;
        _remainingShots = 0;
        _target = null;
        _caster = null;
        _isFiring = false;

        _runtime = null;
    }
}

    

