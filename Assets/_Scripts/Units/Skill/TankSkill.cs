using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class TankSkill : NetworkBehaviour, IHeroSkill
{
    [SerializeField] private TankSkillSO _data;

    private TickTimer _timer;
    private bool _isActive;
    private HeroController _caster;

    public SkillDataSO Data => _data;

    public bool CanUse(HeroController caster, SkillRuntimeData runtime)
    {
        return !_isActive;
    }

    public bool Execute(HeroController caster, SkillRuntimeData runtime)
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

        _isActive = true;
        _timer = TickTimer.CreateFromSeconds(Runner, runtime.Duration);
        _caster.SetDamageReduction(runtime.DamageReductionRate);
        RPC_PlayEffect(caster.Object.Id);

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is TankSkillSO defenseData)
        {
            _data = defenseData;
        }
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
        if (_data.EffectPrefab == null)
        {
            return;
        }

        if (!Runner.TryFindObject(casterId, out NetworkObject casterObject))
        {
            return;
        }

        Transform casterTransform = casterObject.transform;

        GameObject effects = Instantiate(
            _data.EffectPrefab,
            casterTransform.position,
            casterTransform.rotation,
            casterTransform//부모 지정
        );

        //부모에 붙였으므로 로컬 기준으로 정렬
        effects.transform.localPosition = Vector3.zero;
        effects.transform.localRotation = Quaternion.identity;
        effects.transform.localScale = Vector3.one * 17f;

        Destroy(effects, _data.Duration);
    }
}
