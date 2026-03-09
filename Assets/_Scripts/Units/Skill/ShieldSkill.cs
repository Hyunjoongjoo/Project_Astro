using Fusion;
using UnityEngine;

public class ShieldSkill : ISkill
{
    private ShieldSkillSO _data;
    private bool _isCasting;
    private UnitController _cachedUnit;
    private TickTimer _skillCooldown;

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
        Debug.Log("실드 스킬 실행됨");
        _cachedUnit.UnitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);//중복방지
        var modifier = new StatModifier(_data.damageReduction, StatModType.Flat, this);
        _cachedUnit.UnitStat.AddModifier(EffectType.DecreaseDamageTaken, modifier, _data.duration);
        _cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, _cachedUnit.Object.Id, _data.skillType, true, _data.duration);
    }
}
