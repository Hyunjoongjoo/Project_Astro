using Fusion;
using UnityEngine;

public class ShieldSkill : ISkill
{
    private ShieldSkillSO _data;
    private UnitController _cachedUnit;

    private SkillPhase _phase = SkillPhase.Idle;
    private TickTimer _phaseTimer;

    private TickTimer _skillCooldown;
    private TickTimer _aoeTimer;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _phase != SkillPhase.Idle;

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

    public void PreDelay() 
    {
        _cachedUnit.HeroAnimator.SetBool("IsCasting", true);
        _phase = SkillPhase.PreDelay;
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.preDelay);
    }

    public void Casting()
    {
        //_skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);

        _cachedUnit.UnitStat.RemoveModifier(EffectType.DecreaseDamageTaken, this);//중복방지
        var modifier = new StatModifier(_data.damageReduction, StatModType.Flat, this);
        _cachedUnit.UnitStat.AddModifier(EffectType.DecreaseDamageTaken, modifier, _data.duration);

        //_cachedUnit.RPC_PlayChildSkillEffect(_cachedUnit.Object.Id, _cachedUnit.Object.Id, _data.skillType, true, _data.duration);
        
        // 보호막 지속시간 설정
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.duration);
        
        if (_data.aoeDamageRatio > 0)
        {
            // 장판 데미지가 있을 경우 장판 데미지 간격 타이머 설정
            _aoeTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.aoeInterval);
        }
    }

    public void PostDelay() 
    {
        _cachedUnit.HeroAnimator.SetBool("IsCasting", false);
        _phase = SkillPhase.PostDelay;
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.postDelay);
    }

    public void Tick()
    {
        // FSM 기반으로 선딜레이 -> 캐스팅 -> 후딜레이 순으로 타이머를 설정하며 순차 실행
        switch (_phase)
        {
            case SkillPhase.PreDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    Casting();
                break;

            case SkillPhase.Casting:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    PostDelay();

                // 증강 B 없으면 실행 안함
                if (_data.aoeDamageRatio <= 0)
                    return;

                if (_aoeTimer.Expired(_cachedUnit.Runner))
                {
                    DoAOEDamage();

                    _aoeTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.aoeInterval);
                }

                break;

            case SkillPhase.PostDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    _phase = SkillPhase.Idle;
                break;
        }

        //if (!_phaseTimer.IsRunning || _phaseTimer.Expired(_cachedUnit.Runner))
        //{
        //    _cachedUnit.HeroAnimator.SetBool("IsCasting", false);
        //    return;
        //}
        // 스킬 종료
        //if (_phaseTimer.Expired(_cachedUnit.Runner))
        //{
        //    _cachedUnit.HeroAnimator.SetBool("IsCasting", false);
        //    return;
        //}
    }
    private void DoAOEDamage()
    {
        if (!_cachedUnit.Object.HasStateAuthority)
            return;

        float damage = _cachedUnit.UnitStat.Attack.Value * _data.aoeDamageRatio;
        Collider[] hits = Physics.OverlapSphere(
            _cachedUnit.transform.position,
            _data.aoeRange,
            _cachedUnit.TargetLayer);

        foreach (var hit in hits)
        {
            UnitController target = hit.GetComponentInParent<UnitController>();

            if (target == null || target == _cachedUnit)
                continue;
            target.TakeDamage(damage);
        }
    }
}
