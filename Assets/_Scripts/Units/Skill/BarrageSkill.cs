using Fusion;
using UnityEngine;

public class BarrageSkill : NetworkBehaviour, IHeroSkill
{
    [Header("포격 설정")]
    [SerializeField] private int _shotCount = 3;
    [SerializeField] private float _shotInterval = 0.05f;
    [SerializeField] private float _damageRatio = 1.0f;

    [Header("이펙트")]
    [SerializeField] private GameObject _projectileFxPrefab;

    private TickTimer _shotTimer;
    private int _remainingShots;

    private HeroController _caster;
    private UnitBase _target;
    private bool _isFiring;

    public int ShotCount => _shotCount;//나중에 증강에서 탄환수를 늘릴때 사용가능하게

    public bool CanUse(HeroController caster)
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

        return true;
    }

    public bool Execute(HeroController caster)
    {
        if (!CanUse(caster))
        {
            return false;
        }

        _caster = caster;
        _target = caster.CurrentTarget;

        _remainingShots = _shotCount;
        _isFiring = true;

        FireOnce();

        if (_remainingShots > 0)
        {
            _shotTimer = TickTimer.CreateFromSeconds(Runner, _shotInterval);
        }
        else
        {
            StopFiring();
        }

        return true;
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
            _shotTimer = TickTimer.CreateFromSeconds(Runner, _shotInterval);
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

        _caster.ApplyBarrageSkillDamage(_target, _damageRatio);

        if (_projectileFxPrefab != null)
        {
            RPC_FireProjectile(_caster.Object.Id, _target.Object.Id);
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
    private void RPC_FireProjectile(NetworkId casterId, NetworkId targetId)
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
            _projectileFxPrefab,
            start,
            Quaternion.identity
        );

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Fire(end);
        }
    }
}
