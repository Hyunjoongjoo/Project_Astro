using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class DefenseSkill : NetworkBehaviour, IHeroSkill
{
    [Header("방어 설정")]
    [SerializeField] private float _duration = 5f;
    [SerializeField] private float _damageReductionRate = 0.5f; // 50%

    [Header("이펙트")]
    [SerializeField] private GameObject _effectPrefab;

    private TickTimer _timer;
    private bool _isActive;
    private HeroController _caster;

    public bool CanUse(HeroController caster)
    {
        return !_isActive;
    }

    public void Execute(HeroController caster)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return;
        }

        if (_isActive)
        {
            return;
        }

        _caster = caster;

        ActivateDefense();
        RPC_PlayEffect(caster.Object.Id);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (!_isActive)
        {
            return;
        }

        if (_timer.Expired(Runner))
        {
            DeactivateDefense();
        }
    }

    private void ActivateDefense()
    {
        _isActive = true;
        _timer = TickTimer.CreateFromSeconds(Runner, _duration);

        _caster.SetDamageReduction(_damageReductionRate);
    }

    private void DeactivateDefense()
    {
        _isActive = false;

        if (_caster != null)
        {
            _caster.ClearDamageReduction();
            _caster = null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEffect(NetworkId casterId)
    {
        if (_effectPrefab == null)
        {
            return;
        }

        if (!Runner.TryFindObject(casterId, out NetworkObject casterObject))
        {
            return;
        }

        Transform casterTransform = casterObject.transform;

        GameObject effects = Instantiate(
            _effectPrefab,
            casterTransform.position,
            casterTransform.rotation,
            casterTransform//부모 지정
        );

        //부모에 붙였으므로 로컬 기준으로 정렬
        effects.transform.localPosition = Vector3.zero;
        effects.transform.localRotation = Quaternion.identity;
        effects.transform.localScale = Vector3.one;

        Destroy(effects, _duration);
    }
}
