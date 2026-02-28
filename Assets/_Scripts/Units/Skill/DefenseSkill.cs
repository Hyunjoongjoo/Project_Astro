using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class DefenseSkill : NetworkBehaviour, IHeroSkill
{
    [SerializeField] private DefenseSkillSO _data;

    private TickTimer _timer;
    private bool _isActive;
    private HeroController _caster;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster)
    {
        return !_isActive;
    }

    public bool Execute(HeroController caster)
    {
        if (!caster.Object.HasStateAuthority)
        {
            return false;
        }

        if (_isActive)
        {
            return false;
        }

        _caster = caster;

        ActivateDefense();
        RPC_PlayEffect(caster.Object.Id);

        return true;
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
        _timer = TickTimer.CreateFromSeconds(Runner, _data.duration);

        _caster.SetDamageReduction(_data.damageReductionRate);
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
        if (_data.effectPrefab == null)
        {
            return;
        }

        if (!Runner.TryFindObject(casterId, out NetworkObject casterObject))
        {
            return;
        }

        Transform casterTransform = casterObject.transform;

        GameObject effects = Instantiate(
            _data.effectPrefab,
            casterTransform.position,
            casterTransform.rotation,
            casterTransform//부모 지정
        );

        //부모에 붙였으므로 로컬 기준으로 정렬
        effects.transform.localPosition = Vector3.zero;
        effects.transform.localRotation = Quaternion.identity;
        effects.transform.localScale = Vector3.one * 17f;

        Destroy(effects, _data.duration);
    }
}
