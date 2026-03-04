using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class TankSkill : MonoBehaviour, IHeroSkill
{
    [SerializeField] private TankSkillSO _data;

    private StatModifier _damageReductionModifier;
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
        Debug.Log($"[Skill Execute] {caster.name} skill 실행 | EffectPrefab: {runtime.EffectPrefab}");
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
        _timer = TickTimer.CreateFromSeconds(caster.Runner, runtime.Duration);
        _damageReductionModifier = new StatModifier(runtime.DamageReductionRate, StatModType.Flat, this);

        _caster.UnitStat.AddModifier(EffectType.DecreaseDamageTaken, _damageReductionModifier);

        _caster.RPC_PlaySkillEffect(caster.transform.position, caster.transform.rotation, SkillEffectType.Tank, runtime.Duration, runtime.Radius * 6.5f);

        return true;
    }

    public void ChangeSkillData(SkillDataSO newData)
    {
        if (newData is TankSkillSO defenseData)
        {
            _data = defenseData;
        }
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        if (_timer.Expired(_caster.Runner))
        {
            DeactivateDefense();
        }
    }

    private void DeactivateDefense()
    {
        _isActive = false;

        if (_caster != null)
        {
            _caster.UnitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);
            _caster = null;
        }
    }
}
