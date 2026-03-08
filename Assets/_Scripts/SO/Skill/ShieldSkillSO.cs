using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSkillSO", menuName = "Scriptable Objects/Skills/Shield Skill")]
public class ShieldSkillSO : BaseSkillSO
{
    [Header("실드형 스킬의 속성")]
    public float damageReduction;
    public float duration;
    public GameObject shieldVFX;

    public override ISkill CreateInstance(UnitController unit)
    {
        return new ShieldSkill(this, unit);
    }
}
