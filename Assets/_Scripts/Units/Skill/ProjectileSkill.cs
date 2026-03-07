using UnityEngine;

public class ProjectileSkill : ISkill
{
    private ProjectileSkillSO _data;
    private bool _isCasting;

    public BaseSkillSO Data => _data;

    public bool IsCasting => _isCasting;

    public ProjectileSkill(ProjectileSkillSO data)
    {
        _data = data;
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ProjectileSkillSO shieldData)
            _data = shieldData;
        else
            Debug.LogWarning($"[ShieldSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck(HeroController caster)
    {
        return false;
    }
    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        
    }
}
