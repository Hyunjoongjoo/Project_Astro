public interface IHeroSkill
{
    bool CanUse(HeroController caster);//스킬 사용 가능한지 여부
    bool Execute(HeroController caster);//실제 스킬 효과 실행

    //확장대비용
    //bool BlockAttackDuringSkill { get; }//스킬시전 상태일때 공격로직을 막을지 여부, true일때 차단
    //bool BlockMoveDuringSkill { get; }//스킬시전 상태일때 이동을 막을지 여부, true일때 차단
}
