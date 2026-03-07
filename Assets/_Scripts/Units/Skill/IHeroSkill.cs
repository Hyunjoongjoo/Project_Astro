using Fusion;
using UnityEngine;

public interface IHeroSkill
{
    SkillDataSO Data { get; }
    bool CanUse(NewHeroController caster, SkillRuntimeData runtime);//스킬 사용 가능한지 여부
    bool Execute(NewHeroController caster, SkillRuntimeData runtime);//실제 스킬 효과 실행
    void TickSkill(NetworkRunner runner);
    void ChangeSkillData(SkillDataSO newData);

}
