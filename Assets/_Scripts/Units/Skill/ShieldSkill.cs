using Fusion;
using UnityEngine;

public class ShieldSkill : ISkill
{
    private ShieldSkillSO _data;
    private bool _isCasting;
    private UnitController _cachedUnit;
    private TickTimer _skillCooldown;
    private TickTimer _aoeTimer;
    private TickTimer _durationTimer;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ShieldSkill(ShieldSkillSO data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);        
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ShieldSkillSO shieldData)
            _data = shieldData;
        else
            Debug.LogWarning($"[ShieldSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public virtual bool UsingConditionCheck()
    {
        if (_cachedUnit.currentTarget != null &&
            _skillCooldown.ExpiredOrNotRunning(_cachedUnit.Runner))
        {
            _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);
            return true;
        }

        return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        _cachedUnit.UnitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);//중복방지
        var modifier = new StatModifier(_data.damageReduction, StatModType.Flat, this);
        _cachedUnit.UnitStat.AddModifier(EffectType.DecreaseDamageTaken, modifier, _data.duration);
        _cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, _cachedUnit.Object.Id, _data.skillType, true, _data.duration);
        _durationTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.duration);
        if (_data.aoeDamageRatio > 0)
        {
            _aoeTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.aoeInterval);
        }
    }

    public void Tick()
    {
        // 스킬이 시작되지 않았으면 실행 안함
        if (!_durationTimer.IsRunning || _durationTimer.Expired(_cachedUnit.Runner))
            return;
        // 스킬 종료
        if (_durationTimer.Expired(_cachedUnit.Runner))
            return;

        // 증강 B 없으면 실행 안함
        if (_data.aoeDamageRatio <= 0)
            return;

        if (_aoeTimer.Expired(_cachedUnit.Runner))
        {
            DoAOEDamage();

            _aoeTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.aoeInterval);
        }
    }
    private void DoAOEDamage()
    {
        if (!_cachedUnit.Object.HasStateAuthority)
            return;

        float damage = _cachedUnit.UnitStat.Attack.Value * _data.aoeDamageRatio;
        Debug.Log($"[ShieldSkill] AoE Triggered damage:{damage}");
        Collider[] hits = Physics.OverlapSphere(
            _cachedUnit.transform.position,
            _data.aoeRange,
            _cachedUnit.TargetLayer);

        foreach (var hit in hits)
        {
            UnitController target = hit.GetComponentInParent<UnitController>();

            if (target == null || target == _cachedUnit)
                continue;
            Debug.Log($"[ShieldSkill] Hit {target.name}");
            target.TakeDamage(damage);
        }
    }
}
