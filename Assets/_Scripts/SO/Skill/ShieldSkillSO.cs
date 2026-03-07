using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSkillSO", menuName = "Scriptable Objects/Skills/Shield Skill")]
public class ShieldSkillSO : BaseSkillSO
{
    public float damageReduction;
    public float duration;
    public GameObject shieldVFX;

    public override ISkill CreateInstance()
    {
        return new ShieldSkill(this);
    }
}
