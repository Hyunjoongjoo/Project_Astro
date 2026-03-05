using UnityEngine;

public class SkillRuntimeData
{
    public float Damage;
    public float Cooldown;
    public float Radius;
    public float SkillRange;
    public bool IsAreaSkill;
    public GameObject EffectPrefab;
    public float EffectLifeTime;


    //강습형
    public float DamageMultiplier;
    //포격형
    public int ShotCount;
    public float ShotInterval;
    //방어형
    public float Duration;
    public float DamageReductionRate;
    //지원형
    public float HealAmount;
}
