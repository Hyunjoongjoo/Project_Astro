using UnityEngine;

public interface IHeroSkill
{
    SkillDataSO Data { get; }
    bool CanUse(HeroController caster, SkillRuntimeData runtime);//스킬 사용 가능한지 여부
    bool Execute(HeroController caster, SkillRuntimeData runtime);//실제 스킬 효과 실행

    void ChangeSkillData(SkillDataSO newData);

}
