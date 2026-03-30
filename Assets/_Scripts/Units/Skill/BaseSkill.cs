using Fusion;
using UnityEngine;

public abstract class BaseSkill<T> : ISkill where T : BaseSkillSO
{
    protected T _data;
    protected UnitController _cachedUnit;

    protected SkillPhase _phase = SkillPhase.Idle;
    protected TickTimer _phaseTimer;
    protected TickTimer _skillCooldown;

    public BaseSkillSO Data => _data;

    public bool IsCasting => _phase != SkillPhase.Idle;

    protected BaseSkill(T data, UnitController unit)
    {
        _data = data;
        _cachedUnit = unit;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.initCooldown);
    }

    public virtual void ChangeData(BaseSkillSO newData)
    {
        if (newData is T oldData)
            _data = oldData;
        else
            Debug.LogWarning($"[{GetType().Name}] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public virtual void PreDelay()
    {
        _phase = SkillPhase.PreDelay;
        _skillCooldown = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.cooldown);
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.preDelay);

        // 이 아래는 애니메이터 제어
        if (_cachedUnit.UnitType != UnitType.Hero) return;

        if (_data.skillType == SkillType.NormalAttack)
            _cachedUnit.HeroAnimator?.SetBool("IsAttacking", true);
        else
            _cachedUnit.HeroAnimator?.SetBool("IsCasting", true);
    }

    public virtual void PostDelay()
    {
        _phase = SkillPhase.PostDelay;
        _phaseTimer = TickTimer.CreateFromSeconds(_cachedUnit.Runner, _data.postDelay);

        // 이 아래는 애니메이터 제어
        if (_cachedUnit.UnitType != UnitType.Hero) return;

        if (_data.skillType == SkillType.NormalAttack)
            _cachedUnit.HeroAnimator?.SetBool("IsAttacking", false);
        else
            _cachedUnit.HeroAnimator?.SetBool("IsCasting", false);
    }

    public virtual void Tick()
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
                break;

            case SkillPhase.PostDelay:
                if (_phaseTimer.Expired(_cachedUnit.Runner))
                    _phase = SkillPhase.Idle;
                break;
        }
    }
    public void PlayEffectSound()
    {
        AudioManager.Instance.PlaySfx(_data.skillSFX);
    }

    public abstract void Casting();
    public abstract bool UsingConditionCheck();
    public virtual void Initialize() { }
}
