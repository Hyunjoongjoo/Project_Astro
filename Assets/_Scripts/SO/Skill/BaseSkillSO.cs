using UnityEngine;

public abstract class BaseSkillSO : ScriptableObject
{
    public string _skillName;
    public string _skillDescription;
    public string _note;
    public float _initCooldown;
    public float _cooldown;

    public abstract ISkill CreateInstance();
}
