using UnityEngine;

public abstract class BaseSkillSO : ScriptableObject
{
    [Header("스킬 기본 정보")]
    public string heroId;
    public Sprite skillIcon;
    public string skillName;
    public string skillDescription;
    public string note;
    public GameObject skillVFX;
    public AudioClip skillSFX;
    public SkillType skillType;
    [Header("스킬 쿨타임")]
    public float initCooldown;
    public float cooldown;
    [Header("선후딜레이 시간")]
    public float preDelay;
    public float postDelay;
    [Header("스킬 시전 중 다른 액션 차단 여부")]
    public bool blockAction;

    public abstract ISkill CreateInstance(UnitController unit);
}
