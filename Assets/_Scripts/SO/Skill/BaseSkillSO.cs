using UnityEngine;

public abstract class BaseSkillSO : ScriptableObject
{
    public string skillName;
    public string skillDescription;
    public string note;
    public float initCooldown;
    public float cooldown;

    public abstract ISkill CreateInstance(UnitController unit);
}
