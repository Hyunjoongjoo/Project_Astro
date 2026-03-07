using Fusion;
using UnityEngine;

public interface IHeroSkill
{
    SkillDataSO Data { get; }
    bool CanUse(HeroController caster, SkillRuntimeData runtime);//스킬 사용 가능한지 여부
    bool Execute(HeroController caster, SkillRuntimeData runtime);//실제 스킬 효과 실행
    void TickSkill(NetworkRunner runner);
    void ChangeSkillData(SkillDataSO newData);

}

public interface ISkill
{
    BaseSkillSO Data { get; }

    bool IsCasting { get; } // 시전 중인 동안 캐스팅 상태에 머문다.

    /// <summary>
    /// 후딜레이 메서드. Casting 다음 실행됨.
    /// </summary>
    void PostDelay();

    /// <summary>
    /// 시전 메서드. 호출할 때 Casting으로 하지 말고 Execute로 할 것. (시전 앞 뒤 선후딜 적용)
    /// </summary>
    void Casting();

    /// <summary>
    /// 선딜레이 메서드. Casting 이전에 실행됨.
    /// </summary>
    void PreDelay();
    void Execute()
    {
        PreDelay();
        Casting();
        PostDelay();
    }

    bool UsingConditionCheck(HeroController caster);

    void ChangeData(BaseSkillSO newData);
}
