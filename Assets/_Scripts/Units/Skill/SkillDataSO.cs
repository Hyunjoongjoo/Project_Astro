using UnityEngine;

public abstract class SkillDataSO : ScriptableObject
{
    [Header("기본 설정")]
    public float initCooldown;
    public float cooldown;
    public float skillRange;
    public GameObject effectPrefab;
}
