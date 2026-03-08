using UnityEngine;

public class ShieldSkill : ISkill
{
    private ShieldSkillSO _data;
    private bool _isCasting;

    public BaseSkillSO Data => _data;
    public bool IsCasting => _isCasting;

    public ShieldSkill(ShieldSkillSO data, MinionController unit)
    {
        _data = data;
    }

    public void ChangeData(BaseSkillSO newData)
    {
        if (newData is ShieldSkillSO shieldData)
            _data = shieldData;
        else
            Debug.LogWarning($"[ShieldSkill] 잘못된 데이터 타입: {newData.GetType().Name}");
    }

    public bool UsingConditionCheck()
    {
        return false;
    }

    public void PreDelay() { _isCasting = true; }

    public void PostDelay() { _isCasting = false; }

    public void Casting()
    {
        
    }
}
