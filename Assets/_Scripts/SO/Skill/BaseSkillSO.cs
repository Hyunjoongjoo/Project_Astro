using UnityEngine;

public abstract class BaseSkillSO : ScriptableObject
{
    public string skillId;
    public string heroId;
    public string skillName;
    public string skillDescription;
    public string note;
    public GameObject skillVFX;
    public SkillType skillType;
    public float initCooldown;
    public float cooldown;

    public abstract ISkill CreateInstance(UnitController unit);
}
